using Epoxid.CodeGen;
using Epoxid.SyntaxAnalysis;

namespace Epoxid.Tests.CodeGen;

public class TestSymbolTable
{
    [Fact]
    public void Variables()
    {
        const string src = """
        bau = 10
        fluffy = fuzzy
        bauBau = bauBauBau = 3

        def hoeh(iyargh):
            ponDeRing = 30
            def bau():
                print(iyargh + ponDeRing)
        """;

        var module = PythonParser.ParseModuleView(src);
        var st = SymbolTable.Create(module, "baubaubau");

        Assert.Single(st.ChildSymbolTables);
        Assert.Equal(6, st.Symbols.Count);
        Assert.Equal(5, st.Symbols.Count(s => s.Value.Usage.HasFlag(VariableUsages.DefinedLocal)));
        Assert.Equal(5, st.Symbols.Count(s => s.Value.Kind == VariableKind.GlobalExplicit));
        Assert.Equal(1, st.Symbols.Count(s => s.Value.Usage.HasFlag(VariableUsages.Used)));

        var childSt = st.ChildSymbolTables[0];
        Assert.Equal(3, childSt.Symbols.Count);
    }
}
