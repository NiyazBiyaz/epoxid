namespace Epoxid.Runtime.Objects;

public class EpType : EpObject
{
    public string DunderName { get; }
    public EpType[] DunderBases { get; }

    internal EpType(string name, EpType[] bases)
    {
        DunderName = name;
        DunderBases = bases;
    }

    public EpType(string name, EpType[] bases, EpType type)
        : base(type)
    {
        DunderName = name;
        DunderBases = bases;
    }

    #region Methods slots

    // Not supported yet.
    //public FrameKeywordCall? DunderCall { get; set; }

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
