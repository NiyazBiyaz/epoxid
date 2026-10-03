using Epoxid.SourceGeneration;

namespace Epoxid.Runtime.Objects;

[EpoxidType]
public partial class EpType : EpObject
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

    public override string ToString() => $"<class '{DunderName}'>";

    #region Methods slots

    public FrameDunderCall? DunderCall { get; set; }

    public BinaryFunction? DunderAdd { get; set; }

    public BinaryFunction? DunderSub { get; set; }

    public BinaryFunction? DunderMul { get; set; }

    public BinaryFunction? DunderTrueDiv { get; set; }

    public BinaryFunction? DunderMod { get; set; }

    public BinaryFunction? DunderPow { get; set; } // It's not really BinaryFunction because in Python it accepts 3 arguments, but for now...

    public UnaryFunction? DunderBool { get; set; }

    public UnaryFunction? DunderLen { get; set; }

    public BinaryFunction? DunderEq { get; set; } = DunderEqImplementation;

    public BinaryFunction? DunderNe { get; set; } = DunderNeImplementation;

    public BinaryFunction? DunderLt { get; set; }

    public BinaryFunction? DunderGt { get; set; }

    public BinaryFunction? DunderIAdd { get; set; }

    public BinaryFunction? DunderISub { get; set; }

    public BinaryFunction? DunderIMul { get; set; }

    public BinaryFunction? DunderITrueDiv { get; set; }

    public BinaryFunction? DunderIMod { get; set; }

    public BinaryFunction? DunderIPow { get; set; } // See above.

    #endregion
}
