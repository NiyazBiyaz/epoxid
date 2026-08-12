namespace PySharp.Runtime.Objects;

public static class PsConstants
{
    public static PsType Object { get; }
    public static PsType Type { get; }
    public static PsType Str { get; }
    public static PsType Int { get; }
    public static PsType Float { get; }
    public static PsType Bool { get; }
    public static PsType Tuple { get; }
    public static PsType Dict { get; }
    public static PsType NoneType { get; }
    public static PsType EllipsisType { get; }
    public static PsType Function { get; }
    public static PsType NativeFunction { get; }

    public static PsBool True { get; }
    public static PsBool False { get; }
    public static PsNone None { get; }
    public static PsEllipsis Ellipsis { get; }

    static PsConstants()
    {
        Object = new("object", []);
        Type = new("type", [Object]);

        Object.DunderClass = Type;
        Type.DunderClass = Type;

        Str = new("str", [Object], Type)
        {
            DunderAdd = PsString.DunderAddImplementation,
            DunderMul = PsString.DunderMulImplementation,
            DunderLen = PsString.DunderLenImplementation,
            DunderEq = PsString.DunderEqImplementation,
            DunderNe = PsString.DunderNeImplementation,
        };
        Float = new("float", [Object], Type)
        {
            DunderAdd = PsFloat.DunderAddImplementation,
            DunderSub = PsFloat.DunderSubImplementation,
            DunderMul = PsFloat.DunderMulImplementation,
            DunderTrueDiv = PsFloat.DunderTrueDivImplementation,
            DunderPow = PsFloat.DunderPowImplementation,
            DunderBool = PsFloat.DunderBoolImplementation,
            DunderEq = PsFloat.DunderEqImplementation,
            DunderNe = PsFloat.DunderNeImplementation,
        };
        Int = new("int", [Object], Type)
        {
            DunderAdd = PsInteger.DunderAddImplementation,
            DunderSub = PsInteger.DunderSubImplementation,
            DunderMul = PsInteger.DunderMulImplementation,
            DunderTrueDiv = PsInteger.DunderTrueDivImplementation,
            DunderPow = PsInteger.DunderPowImplementation,
            DunderBool = PsInteger.DunderBoolImplementation,
            DunderEq = PsInteger.DunderEqImplementation,
            DunderNe = PsInteger.DunderNeImplementation,
        };
        Bool = new("bool", [Int], Type)
        {
            DunderBool = PsBool.DunderBoolImplementation,
        };

        Tuple = new("tuple", [Object], Type)
        {
            DunderLen = PsTuple.DunderLenImplementation,
        };
        Dict = new("dict", [Object], Type)
        {
            DunderLen = PsDict.DunderLenImplementation,
        };

        NoneType = new("NoneType", [Object], Type)
        {
            DunderBool = PsNone.DunderBoolImplementation,
        };
        EllipsisType = new("EllipsisType", [Object], Type);

        Function = new("function", [Object], Type);
        NativeFunction = new("native_function", [Object], Type);

        True = new(true);
        False = new(false);
        None = new PsNone();
        Ellipsis = new PsEllipsis();
    }
}
