using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Epoxid.SourceGeneration.Tests;

public class CSharpSourceGeneratorVerifier<TGenerator>
    where TGenerator : IIncrementalGenerator, new()
{
    public static async Task VerifyAsync(
        FileValue[] commonSources,
        FileValue[] sources,
        FileValue[] generated)
    {
        var test = new Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
        };

        foreach (var (filename, content) in commonSources)
        {
            test.TestState.Sources.Add((filename, SourceText.From(content, Encoding.UTF8)));
        }

        foreach (var (filename, content) in sources)
        {
            test.TestState.Sources.Add((filename, SourceText.From(content, Encoding.UTF8)));
        }

        foreach (var (filename, content) in generated)
        {
            test.TestState.GeneratedSources.Add((typeof(TGenerator), filename, content));
        }

        await test.RunAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    public class Test : CSharpSourceGeneratorTest<TGenerator, DefaultVerifier>
    {
        protected override ParseOptions CreateParseOptions()
            => ((CSharpParseOptions)base.CreateParseOptions())
                .WithLanguageVersion(LanguageVersion.CSharp14);
    }
}
