using Epoxid.SyntaxAnalysis;
using Epoxid.VM;

namespace Epoxid.CodeGen;

public static class CodeFactory
{
    public static CodeObject CompileModule(FileView file, string moduleName)
    {
        var st = SymbolTable.Create(file, moduleName);
        var cb = new CodeBuilder(st);
        CodeGenerator.Generate(cb, file);
        return cb.Compile();
    }
}
