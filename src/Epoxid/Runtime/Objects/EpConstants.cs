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

        Str = new("str", [Object], Type)
        {
            DunderAdd = EpString.DunderAddImplementation,
            DunderMul = EpString.DunderMulImplementation,
            DunderLen = EpString.DunderLenImplementation,
            DunderEq = EpString.DunderEqImplementation,
            DunderNe = EpString.DunderNeImplementation,
        };
        Float = new("float", [Object], Type)
        {
            DunderAdd = EpFloat.DunderAddImplementation,
            DunderSub = EpFloat.DunderSubImplementation,
            DunderMul = EpFloat.DunderMulImplementation,
            DunderTrueDiv = EpFloat.DunderTrueDivImplementation,
            DunderPow = EpFloat.DunderPowImplementation,
            DunderBool = EpFloat.DunderBoolImplementation,
            DunderEq = EpFloat.DunderEqImplementation,
            DunderNe = EpFloat.DunderNeImplementation,
        };
        Int = new("int", [Object], Type)
        {
            DunderAdd = EpInteger.DunderAddImplementation,
            DunderSub = EpInteger.DunderSubImplementation,
            DunderMul = EpInteger.DunderMulImplementation,
            DunderTrueDiv = EpInteger.DunderTrueDivImplementation,
            DunderPow = EpInteger.DunderPowImplementation,
            DunderBool = EpInteger.DunderBoolImplementation,
            DunderEq = EpInteger.DunderEqImplementation,
            DunderNe = EpInteger.DunderNeImplementation,
        };
        Bool = new("bool", [Int], Type)
        {
            DunderBool = EpBool.DunderBoolImplementation,
        };

        Tuple = new("tuple", [Object], Type)
        {
            DunderLen = EpTuple.DunderLenImplementation,
        };
        Dict = new("dict", [Object], Type)
        {
            DunderLen = EpDict.DunderLenImplementation,
        };

        NoneType = new("NoneType", [Object], Type)
        {
            DunderBool = EpNone.DunderBoolImplementation,
        };
        EllipsisType = new("EllipsisType", [Object], Type);

        Function = new("function", [Object], Type);
        NativeFunction = new("native_function", [Object], Type);

        True = new(true);
        False = new(false);
        None = new EpNone();
        Ellipsis = new EpEllipsis();
    }
}
