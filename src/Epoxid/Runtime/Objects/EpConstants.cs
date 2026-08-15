namespace Epoxid.Runtime.Objects;

public static class EpConstants
{
    public static EpType Object { get; }
    public static EpType Type { get; }
    public static EpType Str { get; }
    public static EpType Int { get; }
    public static EpType Float { get; }
    public static EpType Bool { get; }
    public static EpType Tuple { get; }
    public static EpType Dict { get; }
    public static EpType NoneType { get; }
    public static EpType EllipsisType { get; }
    public static EpType Function { get; }
    public static EpType NativeFunction { get; }

    public static EpBool True { get; }
    public static EpBool False { get; }
    public static EpNone None { get; }
    public static EpEllipsis Ellipsis { get; }

    static EpConstants()
    {
        Object = new("object", []);
        Type = new("type", [Object]);

        Object.DunderClass = Type;
        Type.DunderClass = Type;

        Str = EpString.Type;
        Float = EpFloat.Type;
        Int = EpInteger.Type;
        Bool = EpBool.Type;

        Tuple = EpTuple.Type;
        Dict = EpDict.Type;

        NoneType = EpNone.Type;
        EllipsisType = EpEllipsis.Type;

        Function = new("function", [Object], Type);
        NativeFunction = new("native_function", [Object], Type);

        True = new(true);
        False = new(false);
        None = new EpNone();
        Ellipsis = new EpEllipsis();
    }
}
