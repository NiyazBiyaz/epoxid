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

    public Func<PsObject, PsObject, PsObject?, PsObject>? DunderCall { get; set; }

    public Func<PsObject, PsObject, PsObject>? DunderAdd { get; set; }

    public Func<PsObject, PsObject, PsObject>? DunderSub { get; set; }

    public Func<PsObject, PsObject, PsObject>? DunderMul { get; set; }

    public Func<PsObject, PsObject, PsObject>? DunderTrueDiv { get; set; }

    public Func<PsObject, PsObject, PsObject>? DunderPow { get; set; }

    #endregion
}
