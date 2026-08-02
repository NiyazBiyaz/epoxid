using Test = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<
    PySharp.SyntaxAnalysis.Generator.Analyzers.AstSwitchAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace PySharp.SyntaxAnalysis.Generator.Analyzers.Tests;

public class TestAnalyzer
{
    [Fact]
    public async Task Test_PGNT001_OmittedInterfaceImplementor()
    {
        const string src = """
        {|PGNT001:switch (bauBau)
        {
            case FuzzyView:
                break;
            default:
                break;
        }|}
        """;
        var test = prepareTest(wrap(src));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Test_PGNT001_OmittedInSecondGeneration()
    {
        const string src = """
        {|PGNT001:switch (bauBau)
        {
            case FuzzyView:
                break;
            case FuwView:
                break;
            default:
                break;
        }|}
        """;
        var test = prepareTest(wrap(src));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Test_PGNT001_WorksForExpression()
    {
        const string src = """
        var haeh = {|PGNT001:bauBau switch
        {
            FuzzyView f => f,
            _ => throw new System.Exception("Bau bau!"),
        }|};
        """;
        var test = prepareTest(wrap(src));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Test_NoDiagnostics_InFirstGeneration()
    {
        const string src = """
        switch (bauBau)
        {
            case FuzzyView:
                break;
            case FluffyView m:
                break;
            default:
                break;
        }
        """;
        var test = prepareTest(wrap(src));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Test_NoDiagnostics_HasSecondGeneration()
    {
        const string src = """
        switch (bauBau)
        {
            case FuzzyView f:
                break;
            case FuwView:
                break;
            case AwaView:
                break;
            default:
                break;
        }
        """;
        var test = prepareTest(wrap(src));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Test_NoDiagnostics_InBothGenerations()
    {
        const string src = """
        switch (bauBau)
        {
            case FuzzyView f:
                break;
            case FuwView:
                break;
            case FluffyView:
                break;
            default:
                break;
        }
        """;
        var test = prepareTest(wrap(src));
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    private static Test prepareTest(string src)
    {
        var test = new Test()
        {
            TestCode = src,
        };
        test.TestState.Sources.Add(declarations_footnote);
        return test;
    }

    private static string wrap(string code) => $$"""
    namespace Bau
    {
        class Mococo
        {
            void Haeh(BauBauView bauBau)
            {
                {{code}}
            }
        }
    }
    """;

    private const string declarations_footnote = """

    using System;

    namespace PySharp.SyntaxAnalysis
    {
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class BaseRuleAttribute(params Type[] members) : Attribute
        {
            public readonly Type[] Members = members;
        }

        [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
        public sealed class WildUnionAttribute(params Type[] members) : Attribute
        {
            public readonly Type[] Members = members;
        }
    }

    namespace Bau
    {
        [PySharp.SyntaxAnalysis.WildUnion(typeof(FuzzyView), typeof(FluffyView))]
        interface BauBauView;

        [PySharp.SyntaxAnalysis.BaseRule(typeof(FuwView), typeof(AwaView))]
        class FluffyView;

        class FuwView : FluffyView;

        class AwaView : FluffyView;

        class FuzzyView;
    }

    """;
}
