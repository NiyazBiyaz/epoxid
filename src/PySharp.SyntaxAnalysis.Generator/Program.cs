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

        Option<bool> forceOption = new("--force", "-f")
        {
            Description = "Force to overwrite parser file if it exists.",
        };

        Option<bool> splitFilesOption = new("--split-files")
        {
            Description = "Create separate files for parser and each AST node.",
        };

        Option<bool> blockNamespaceOption = new("--use-block-namespaces")
        {
            Description = "Enable output code to have block namespaces.",
        };

        root.Add(forceOption);
        root.Add(splitFilesOption);
        root.Add(blockNamespaceOption);

        root.SetAction(parseResult =>
        {
            string? output = parseResult.GetValue(parserOutput);
            var grammarFile = parseResult.GetValue(grammarInput) ?? throw new NullReferenceException("Given argument is null.");
            bool forced = parseResult.GetValue(forceOption);
            bool splitFiles = parseResult.GetValue(splitFilesOption);
            bool blockNamespace = parseResult.GetValue(blockNamespaceOption);

            if (!grammarFile.Exists)
            {
                Console.Error.WriteLine($"File '{grammarFile.FullName}' does not exists.");
                Environment.Exit(1);
            }

            if (output != null && splitFiles)
            {
                if (!Directory.Exists(output))
                    Directory.CreateDirectory(output);
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

            // TODO: move it to the CsGenerator
            if (splitFiles)
            {
                foreach (var type in boundGrammar.Types)
                {
                    string outputPath = type.Name + ".g.cs";
                    if (output != null)
                        outputPath = Path.Combine(output, outputPath);

                    var fileGenerator = new CsGenerator(boundGrammar.AccessModifier);

                    fileGenerator.AddFileHeader(boundGrammar.UserHeader, grammarFile.Name);

                    if (blockNamespace)
                    {
                        fileGenerator.AddLine($"namespace {boundGrammar.Namespace}");
                        using (fileGenerator.CreateBlock())
                        {
                            fileGenerator.AddType(type.ToIr());
                        }
                    }
                    else
                    {
                        fileGenerator.AddLine($"namespace {boundGrammar.Namespace};");
                        fileGenerator.AddType(type.ToIr());
                    }

                    File.WriteAllText(outputPath, fileGenerator.Dump());
                }

                {
                    string outputPath = boundGrammar.ParserName + ".g.cs";
                    if (output != null)
                        outputPath = Path.Combine(output, outputPath);

                    var fileGenerator = new CsGenerator(boundGrammar.AccessModifier);

                    fileGenerator.AddFileHeader(boundGrammar.UserHeader, grammarFile.Name);

                    if (blockNamespace)
                    {
                        fileGenerator.AddLine($"namespace {boundGrammar.Namespace}");
                        using (fileGenerator.CreateBlock())
                        {
                            fileGenerator.AddParserSignature(boundGrammar.ParserName, boundGrammar.TopLevelNodeName);
                            fileGenerator.AddParserBody(
                                boundGrammar.MainRule.Name,
                                boundGrammar.TopLevelNodeName,
                                boundGrammar.Rules.Select(r => r.ToIr()),
                                []);
                        }
                    }
                    else
                    {
                        fileGenerator.AddLine($"namespace {boundGrammar.Namespace};");
                        fileGenerator.AddParserSignature(boundGrammar.ParserName, boundGrammar.TopLevelNodeName);
                        fileGenerator.AddParserBody(
                            boundGrammar.MainRule.Name,
                            boundGrammar.TopLevelNodeName,
                            boundGrammar.Rules.Select(r => r.ToIr()),
                            []);
                    }
                    File.WriteAllText(outputPath, fileGenerator.Dump());
                }
            }
            else
            {
                var fileGenerator = new CsGenerator(boundGrammar.AccessModifier);

                fileGenerator.AddFileHeader(boundGrammar.UserHeader, grammarFile.Name);
                string generatedGrammar = boundGrammar.GenerateCode();

                if (blockNamespace)
                {
                    fileGenerator.AddLine($"namespace {boundGrammar.Namespace}");
                    using (fileGenerator.CreateBlock())
                    {
                        fileGenerator.AddFileBody(generatedGrammar);
                    }
                }
                else
                {
                    fileGenerator.AddLine($"namespace {boundGrammar.Namespace};");
                    fileGenerator.AddFileBody(generatedGrammar);
                }

                string outputPath = output ?? boundGrammar.ParserName + ".g.cs";

                if (File.Exists(outputPath))
                {
                    Console.Error.WriteLine($"File '{outputPath}' already exists. Use --force flag to overwrite it.");
                    Environment.Exit(1);
                }

                File.WriteAllText(outputPath, fileGenerator.Dump());
            }
        });

        root.Parse(args).Invoke();
    }
}
