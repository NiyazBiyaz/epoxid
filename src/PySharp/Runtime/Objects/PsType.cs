namespace PySharp.Runtime.Objects;

public class PsType : PsObject
{
    public string DunderName { get; }
    public PsType[] DunderBases { get; }

    internal PsType(string name, PsType[] bases)
    {
        DunderName = name;
        DunderBases = bases;
    }

    public PsType(string name, PsType[] bases, PsType type)
        : base(type)
    {
        DunderName = name;
        DunderBases = bases;
    }

    #region Methods slots

    public TernaryFunction? DunderCall { get; set; }

    public BinaryFunction? DunderAdd { get; set; }

    public BinaryFunction? DunderSub { get; set; }

    public BinaryFunction? DunderMul { get; set; }

    public BinaryFunction? DunderTrueDiv { get; set; }

    public BinaryFunction? DunderPow { get; set; } // It's not really BinaryFunction because in Python it accepts 3 arguments, but for now...

    public UnaryFunction? DunderBool { get; set; }

    public UnaryFunction? DunderLen { get; set; }

    public BinaryFunction? DunderEq { get; set; } = DunderEqImplementation;

    public BinaryFunction? DunderNe { get; set; } = DunderNeImplementation;

    #endregion
}
