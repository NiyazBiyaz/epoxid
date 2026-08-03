using System.CommandLine;
using System.Runtime.CompilerServices;
using PySharp.SyntaxAnalysis.Common;
using PySharp.SyntaxAnalysis.Tokens;

[assembly: InternalsVisibleTo("PySharp.SyntaxAnalysis.Generator.Tests")]

namespace PySharp.SyntaxAnalysis.Generator;

internal class Program
{
    private static void Main(string[] args)
    {
        RootCommand root = new("PEG parser compiler-like generator PegenNet inspired by CPython's pegen.");

        Argument<FileInfo> grammarInput = new("Grammar file")
        {
            Description = "Path to the grammar file generate parser to.",
        };

        root.Add(grammarInput);

        Option<string> parserOutput = new("--output", "-o")
        {
            Description = "Path to file of generated parser.",
        };

        root.Add(parserOutput);

        Option<bool> splitFilesOption = new("--split-files")
        {
            Description = "Create separate files for parser and each AST node.",
        };

        Option<bool> blockNamespaceOption = new("--use-block-namespaces")
        {
            Description = "Enable output code to have block namespaces.",
        };

        root.Add(splitFilesOption);
        root.Add(blockNamespaceOption);

        root.SetAction(parseResult =>
        {
            string? output = parseResult.GetValue(parserOutput);
            var grammarFile = parseResult.GetValue(grammarInput) ?? throw new NullReferenceException("Given argument is null.");
            bool splitFiles = parseResult.GetValue(splitFilesOption);
            bool blockNamespace = parseResult.GetValue(blockNamespaceOption);

            if (!grammarFile.Exists)
            {
                Console.Error.WriteLine($"File '{grammarFile.FullName}' does not exists.");
                Environment.Exit(1);
            }

            string grammar = grammarFile.OpenText().ReadToEnd();

            var gramBuffer = new StringBuffer(grammar);
            var tokenizer = new Tokenizer(SynchronizationPoint.ClearPoint(gramBuffer));
            var tokenStream = new TokenNodeStream(tokenizer);
            var parser = new GrammarParser(tokenStream);

            var grammarParsed = parser.Parse();

            if (grammarParsed is null)
            {
                Console.Error.WriteLine($"Parsing error. Line: {tokenizer.Synchronize().StartLine + 1}");
                Environment.Exit(1);
            }

            var binder = new Binder();

            var grammarView = grammarParsed.GetView(0, null);
            grammarView.SyntaxTree = new SyntaxViewTree
            {
                Root = grammarView,
                PositionMap = tokenizer.PositionMap,
            };

            try
            {
                binder.ReadMetadata(grammarView.Metadata);
                binder.ReadKeywords(grammarView);
                binder.RegisterRules(grammarView.Rules);
                binder.PopulateRules();
                binder.CreateTypes();
                binder.InspectRules();
            }
            catch (CompilationException e)
            {
                Console.Error.WriteLine($"Error at line {e.Line + 1}: {e.Message}");
                Environment.Exit(1);
            }

            foreach (var warn in binder.Warnings)
            {
                Console.WriteLine($"Warning at line {warn.Line + 1}: {warn.Message}");
            }

            var boundGrammar = binder.Grammar;

            if (splitFiles)
            {
                if (output != null && !Directory.Exists(output))
                {
                    Directory.CreateDirectory(output);
                }

                if (output != null)
                {
                    output += Path.DirectorySeparatorChar;
                }

                string parserOutput = output + boundGrammar.ParserName + ".g.cs";

                var grammarIr = boundGrammar.ToIr();

                var parserGenerator = new CsGenerator(boundGrammar.AccessModifier);

                parserGenerator.AddFileHeader(boundGrammar.UserHeader, grammarFile.Name);
                parserGenerator.SetupNamespace(boundGrammar.Namespace, blockNamespace);
                parserGenerator.AddParser(grammarIr);

                File.WriteAllText(parserOutput, parserGenerator.Dump());

                foreach (var type in grammarIr.Types)
                {
                    string typeOutput = output + type.Name + ".g.cs";

                    var typeGenerator = new CsGenerator(boundGrammar.AccessModifier);

                    typeGenerator.AddFileHeader(boundGrammar.UserHeader, grammarFile.Name);
                    typeGenerator.SetupNamespace(boundGrammar.Namespace, blockNamespace);
                    typeGenerator.AddType(type);

                    File.WriteAllText(typeOutput, typeGenerator.Dump());
                }
            }
            else
            {
                output ??= boundGrammar.ParserName + ".g.cs";

                var grammarIr = boundGrammar.ToIr();

                var parserGenerator = new CsGenerator(boundGrammar.AccessModifier);

                parserGenerator.AddFileHeader(boundGrammar.UserHeader, grammarFile.Name);
                parserGenerator.SetupNamespace(boundGrammar.Namespace, blockNamespace);
                parserGenerator.AddParser(grammarIr);
                parserGenerator.AddTypes(grammarIr.Types);

                File.WriteAllText(output, parserGenerator.Dump());
            }
        });

        root.Parse(args).Invoke();
    }
}
