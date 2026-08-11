namespace PySharp.SyntaxAnalysis;

internal abstract record ValidationResult
{
    // TODO: file location
    public sealed record Success : ValidationResult;
    public sealed record Warning(string Message) : ValidationResult;
    public sealed record Error(string Message) : ValidationResult;

    public static readonly Success ResultSuccess = new();
    public static readonly Error ErrorDefaultOrder = new("parameter without a default follows parameter with a default");
    public static readonly Error ErrorInvalidSlash = new("positional-only marker cannot be used twice");
    public static readonly Error ErrorNeedParamAfterStar = new("at least one parameter must follow bare '*'");
}
