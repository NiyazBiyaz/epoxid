namespace Epoxid.CodeGen;

internal abstract record ValidationResult
{
    // TODO: file location
    public sealed record Success : ValidationResult;
    public sealed record Error(string Message) : ValidationResult;

    public static readonly Success ResultSuccess = new();
    public static readonly Error ErrorDefaultOrder = new("lkj");
    public static readonly Error ErrorInvalidSlash = new("jlkdjf");
    public static readonly Error ErrorNeedParamAfterStar = new("jfldksjf");
}
