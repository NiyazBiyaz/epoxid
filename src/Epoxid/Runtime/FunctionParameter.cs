namespace Epoxid.Runtime;

public readonly record struct FunctionParameter(string Name, bool Required)
{
    public static readonly FunctionParameter Args = new("args", false);
    public static readonly FunctionParameter Kwargs = new("kwargs", false);

    public static FunctionParameter Variadic(string name) => new(name, false);
}
