using Epoxid.SourceGeneration;

namespace Epoxid.Runtime.Objects;

[EpoxidType]
public partial class EpType : EpObject
{
    [Slot("__name__")]
    public partial string DunderName { get; private set; }

    [Slot("__bases__")]
    public partial EpType[] DunderBases { get; private set; }

    internal EpType(string name, EpType[] bases)
        : base()
    {
        setupSlots();
        DunderName = name;
        DunderBases = bases;
    }

    public EpType(string name, EpType[] bases, EpType type)
        : base(type)
    {
        setupSlots();
        DunderName = name;
        DunderBases = bases;
    }

    private void setupSlots()
    {
        DunderEq = DunderEqImplementation;
        DunderNe = DunderNeImplementation;
        DunderNew = DunderNewImplementation;

        DunderDict ??= [];

        foreach (var (descriptor, offset) in slot_descriptors_mapping)
        {
            if (!DunderDict.TryAdd((EpString)descriptor, new EpWrapperObject(offset)))
            {
                throw new InvalidOperationException($"Cannot add slot descriptor: __dict__ already have record for '{descriptor}'");
            }
        }
    }

    public override string ToString() => $"<class '{DunderName}'>";

    #region Methods slots

    [Slot("__new__")]
    public partial UnaryFunction DunderNew { get; set; }

    [Slot("__call__")]
    public partial FrameDunderCall? DunderCall { get; set; }

    [Slot("__add__")]
    public partial BinaryFunction? DunderAdd { get; set; }

    [Slot("__sub__")]
    public partial BinaryFunction? DunderSub { get; set; }

    [Slot("__mul__")]
    public partial BinaryFunction? DunderMul { get; set; }

    [Slot("__truediv__")]
    public partial BinaryFunction? DunderTrueDiv { get; set; }

    [Slot("__mod__")]
    public partial BinaryFunction? DunderMod { get; set; }

    [Slot("__pow__")]
    public partial BinaryFunction? DunderPow { get; set; } // It's not really BinaryFunction because in Python it accepts 3 arguments, but for now...

    [Slot("__bool__")]
    public partial UnaryFunction? DunderBool { get; set; }

    [Slot("__len__")]
    public partial UnaryFunction? DunderLen { get; set; }

    [Slot("__eq__")]
    public partial BinaryFunction? DunderEq { get; set; }

    [Slot("__ne__")]
    public partial BinaryFunction? DunderNe { get; set; }

    [Slot("__lt__")]
    public partial BinaryFunction? DunderLt { get; set; }

    [Slot("__gt__")]
    public partial BinaryFunction? DunderGt { get; set; }

    [Slot("__iadd__")]
    public partial BinaryFunction? DunderIAdd { get; set; }

    [Slot("__isub__")]
    public partial BinaryFunction? DunderISub { get; set; }

    [Slot("__imul__")]
    public partial BinaryFunction? DunderIMul { get; set; }

    [Slot("__itruediv__")]
    public partial BinaryFunction? DunderITrueDiv { get; set; }

    [Slot("__imod__")]
    public partial BinaryFunction? DunderIMod { get; set; }

    [Slot("__ipow__")]
    public partial BinaryFunction? DunderIPow { get; set; } // See above.

    #endregion
}
