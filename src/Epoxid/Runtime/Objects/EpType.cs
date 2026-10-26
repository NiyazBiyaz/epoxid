using System.Diagnostics;
using Epoxid.SourceGeneration;

namespace Epoxid.Runtime.Objects;

[EpoxidType]
public partial class EpType : EpObject
{
    [Slot("__name__")]
    public string DunderName { get; internal set; } = null!;

    [Slot("__bases__")]
    public EpType[] DunderBases { get; internal set; } = null!;

    public static EpType Type { get; private set; } = null!;

    internal static new void Ready()
    {
        Debug.Assert(RuntimeInitializer.IsInitialized);

        Type = RuntimeInitializer.Type;

        Type.DunderCall = dunderCallImplementation;
    }

    internal static EpType EmptyCtor() => new();

    internal void Initialize(string name, EpType[] bases)
    {
        DunderName = name;
        DunderBases = bases;
        DunderMro = Core.MethodResolutionOrder(this);
    }

    internal void EnrichSlots(IReadOnlyList<KnownSlotDescriptor>? knownSlots = null)
    {
        var slots = knownSlots ?? (this == Type ? known_slots : []);

        DunderDict ??= [];
        foreach (var descriptor in slots)
        {
            var slotName = new EpString(descriptor.DescriptorName);
            if (!DunderDict.TryAdd(slotName, new WrapperObject<KnownSlotDescriptor>(descriptor)))
            {
                throw new InvalidOperationException($"Cannot add slot descriptor: __dict__ already have record for {slotName}");
            }
        }

        copyMethods();
    }

    private void copyMethods()
    {
        if (this == Type)
        {
            postInitialization();
            return;
        }
    }

    /// <summary>
    /// Copy manually managed methods from EpObject.Object to EpType.Type
    /// </summary>
    private void postInitialization()
    {
        DunderEq = Object.DunderEq;
        DunderNe = Object.DunderNe;
        DunderNew = Object.DunderNew;
        DunderGetAttribute = Object.DunderGetAttribute;
        DunderSetAttr = Object.DunderSetAttr;
    }

    private EpType()
        : base()
    {
    }

    public EpType(string name, EpType[] bases, EpType type)
        : base(type)
    {
        Initialize(name, bases);
        EnrichSlots();
    }

    public override string ToString() => $"<class '{DunderName}'>";

    private static EpObject dunderCallImplementation(EpObject self, ReadOnlySpan<EpObject> args)
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

    [Slot("__mro__")]
    public EpType[] DunderMro { get; internal set; } = null!;

    [Slot("__new__")]
    public UnaryFunction DunderNew { get; set; } = null!;

    [Slot("__call__")]
    public FrameDunderCall? DunderCall { get; set; }

    [Slot("__init__")]
    public FrameDunderCall? DunderInit { get; set; }

    [Slot("__add__")]
    public BinaryFunction? DunderAdd { get; set; }

    [Slot("__sub__")]
    public BinaryFunction? DunderSub { get; set; }

    [Slot("__mul__")]
    public BinaryFunction? DunderMul { get; set; }

    [Slot("__truediv__")]
    public BinaryFunction? DunderTrueDiv { get; set; }

    [Slot("__mod__")]
    public BinaryFunction? DunderMod { get; set; }

    [Slot("__pow__")]
    public BinaryFunction? DunderPow { get; set; } // It's not really BinaryFunction because in Python it accepts 3 arguments, but for now...

    [Slot("__bool__")]
    public UnaryFunction? DunderBool { get; set; }

    [Slot("__len__")]
    public UnaryFunction? DunderLen { get; set; }

    [Slot("__eq__")]
    public BinaryFunction? DunderEq { get; set; }

    [Slot("__ne__")]
    public BinaryFunction? DunderNe { get; set; }

    [Slot("__lt__")]
    public BinaryFunction? DunderLt { get; set; }

    [Slot("__gt__")]
    public BinaryFunction? DunderGt { get; set; }

    [Slot("__iadd__")]
    public BinaryFunction? DunderIAdd { get; set; }

    [Slot("__isub__")]
    public BinaryFunction? DunderISub { get; set; }

    [Slot("__imul__")]
    public BinaryFunction? DunderIMul { get; set; }

    [Slot("__itruediv__")]
    public BinaryFunction? DunderITrueDiv { get; set; }

    [Slot("__imod__")]
    public BinaryFunction? DunderIMod { get; set; }

    [Slot("__ipow__")]
    public BinaryFunction? DunderIPow { get; set; } // See above.

    [Slot("__get__")]
    public DescriptorGetFunction? DunderGet { get; set; }

    [Slot("__set__")]
    public DescriptorSetFunction? DunderSet { get; set; }

    [Slot("__getattribute__")]
    public GetAttributeFunction DunderGetAttribute { get; set; } = null!;

    [Slot("__getattr__")]
    public GetAttributeFunction? DunderGetAttr { get; set; }

    [Slot("__setattr__")]
    public SetAttributeFunction DunderSetAttr { get; set; } = null!;

    #endregion
}
