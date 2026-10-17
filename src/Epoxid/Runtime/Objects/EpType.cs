using Epoxid.SourceGeneration;

namespace Epoxid.Runtime.Objects;

[EpoxidType]
public partial class EpType : EpObject
{
    [Slot("__name__")]
    public partial string DunderName { get; internal set; }

    [Slot("__bases__")]
    public partial EpType[] DunderBases { get; internal set; }

    public static readonly EpType Type = RuntimeInitializer.Type;

    internal EpType(string name, EpType[] bases)
        : base()
    {
        FillSlots();
        DunderName = name;
        DunderBases = bases;
    }

    internal static EpType EmptyCtor() => new();

    private EpType()
        : base()
    {
    }

    public EpType(string name, EpType[] bases, EpType type)
        : base(type)
    {
        FillSlots();
        DunderName = name;
        DunderBases = bases;
    }

    internal void FillSlots()
    {
        DunderDict ??= [];

        foreach (var (descriptorName, offset) in slot_descriptors_mapping)
        {
            var slotDescriptor = new EpSlotDescriptor(offset, descriptorName);
            if (!DunderDict.TryAdd((EpString)descriptorName, slotDescriptor))
            {
                throw new InvalidOperationException($"Cannot add slot descriptor: __dict__ already have record for '{descriptorName}'");
            }
        }

        DunderCall = dunderCallImplementation;
    }

    public override string ToString() => $"<class '{DunderName}'>";

    private EpObject dunderCallImplementation(EpObject self, ReadOnlySpan<EpObject> args)
    {
        switch (self)
        {
            case EpType typeItself when typeItself == Type:

                throw new NotImplementedException();

            case EpType otherType:
            {
                var newObject = otherType.DunderNew(otherType);

                otherType.DunderInit?.Invoke(newObject, args);

                return newObject;
            }

            default:
                throw new ArgumentException("'self' is not a type object");
        }
    }

    #region Methods slots

    [Slot("__new__")]
    public partial UnaryFunction DunderNew { get; set; }

    [Slot("__call__")]
    public partial FrameDunderCall? DunderCall { get; set; }

    [Slot("__init__")]
    public partial FrameDunderCall? DunderInit { get; set; }

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

    [Slot("__get__")]
    public partial DescriptorGetFunction? DunderGet { get; set; }

    [Slot("__set__")]
    public partial DescriptorSetFunction? DunderSet { get; set; }

    [Slot("__getattribute__")]
    public partial GetAttributeFunction DunderGetAttribute { get; set; }

    [Slot("__getattr__")]
    public partial GetAttributeFunction? DunderGetAttr { get; set; }

    [Slot("__setattr__")]
    public partial SetAttributeFunction DunderSetAttr { get; set; }

    #endregion
}
