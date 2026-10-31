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

    [Slot("__mro__")]
    public EpType[] DunderMro { get; internal set; } = null!;

    public static EpType Type { get; private set; } = null!;

    internal static new void Ready()
    {
        Debug.Assert(RuntimeInitializer.IsInitialized);

        Type = RuntimeInitializer.Type;

        Type.DunderCall = dunderCallImplementation;
    }

    internal void Initialize(string name, EpType[] bases)
    {
        DunderName = name;
        DunderBases = bases;
        DunderMro = Core.MethodResolutionOrder(this);
    }

    internal void FillSlots(IReadOnlyList<KnownSlotDescriptor>? additionalSlots = null)
    {
        DunderDict ??= [];
        foreach (var descriptor in known_slots)
        {
            if (getKnownSlot(descriptor.DescriptorName) != null)
            {
                var slotName = new EpString(descriptor.DescriptorName);
                if (!DunderDict.TryAdd(slotName, descriptor))
                {
                    throw new InvalidOperationException($"Cannot add slot descriptor: __dict__ already have record for '{slotName}'");
                }
            }
        }

        foreach (var descriptor in additionalSlots ?? [])
        {
            var slotName = new EpString(descriptor.DescriptorName);
            if (!DunderDict.TryAdd(slotName, descriptor))
            {
                throw new InvalidOperationException($"Cannot add slot descriptor: __dict__ already have record for '{slotName}'");
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

        foreach (var baseType in DunderMro)
        {
            borrowDunderMethods(baseType);
        }
    }

    private void borrowDunderMethods(EpType baseType)
    {
        foreach (var slotDescriptor in known_slots)
        {
            string dunderSlotName = slotDescriptor.DescriptorName;
            bool canBeCopied = isDunderMethodName(dunderSlotName)
                            && getKnownSlot(dunderSlotName) == null
                            && baseType.getKnownSlot(dunderSlotName) != null
                            && baseType.ownsDunderMethod(dunderSlotName);

            if (canBeCopied)
            {
                copyDunderMethod(baseType, dunderSlotName);
            }
        }
    }

    private bool ownsDunderMethod(string methodName)
    {
        Debug.Assert(DunderDict != null);
        var lookup = DunderDict.GetStringLookup();
        return lookup.ContainsKey(methodName);
    }

    private void copyDunderMethod(EpType type, ReadOnlySpan<char> methodName)
    {
        switch (methodName)
        {
            case "__name__":
            case "__bases__":
            case "__mro__":
                throw new ArgumentException($"Invalid method to copy: '{methodName}'");

            case "__new__":
                DunderNew = type.DunderNew;
                break;
            case "__call__":
                DunderCall = type.DunderCall;
                break;
            case "__init__":
                DunderInit = type.DunderInit;
                break;
            case "__add__":
                DunderAdd = type.DunderAdd;
                break;
            case "__sub__":
                DunderSub = type.DunderSub;
                break;
            case "__mul__":
                DunderMul = type.DunderMul;
                break;
            case "__truediv__":
                DunderTrueDiv = type.DunderTrueDiv;
                break;
            case "__mod__":
                DunderMod = type.DunderMod;
                break;
            case "__pow__":
                DunderPow = type.DunderPow;
                break;
            case "__bool__":
                DunderBool = type.DunderBool;
                break;
            case "__len__":
                DunderLen = type.DunderLen;
                break;
            case "__eq__":
                DunderEq = type.DunderEq;
                break;
            case "__ne__":
                DunderNe = type.DunderNe;
                break;
            case "__lt__":
                DunderLt = type.DunderLt;
                break;
            case "__gt__":
                DunderGt = type.DunderGt;
                break;
            case "__iadd__":
                DunderIAdd = type.DunderIAdd;
                break;
            case "__isub__":
                DunderISub = type.DunderISub;
                break;
            case "__imul__":
                DunderIMul = type.DunderIMul;
                break;
            case "__itruediv__":
                DunderITrueDiv = type.DunderITrueDiv;
                break;
            case "__imod__":
                DunderIMod = type.DunderIMod;
                break;
            case "__ipow__":
                DunderIPow = type.DunderIPow;
                break;
            case "__get__":
                DunderGet = type.DunderGet;
                break;
            case "__set__":
                DunderSet = type.DunderSet;
                break;
            case "__getattribute__":
                DunderGetAttribute = type.DunderGetAttribute;
                break;
            case "__getattr__":
                DunderGetAttr = type.DunderGetAttr;
                break;
            case "__setattr__":
                DunderSetAttr = type.DunderSetAttr;
                break;

            default:
                throw new ArgumentException($"Unhandled method name: {methodName}");
        }
    }

    private object? getKnownSlot(ReadOnlySpan<char> methodName)
    {
        return methodName switch
        {
            // Not methods, but would appear as slots
            "__name__" => DunderName,
            "__bases__" => DunderBases,
            "__mro__" => DunderMro,
            "__new__" => DunderNew,
            "__call__" => DunderCall,
            "__init__" => DunderInit,
            "__add__" => DunderAdd,
            "__sub__" => DunderSub,
            "__mul__" => DunderMul,
            "__truediv__" => DunderTrueDiv,
            "__mod__" => DunderMod,
            "__pow__" => DunderPow,
            "__bool__" => DunderBool,
            "__len__" => DunderLen,
            "__eq__" => DunderEq,
            "__ne__" => DunderNe,
            "__lt__" => DunderLt,
            "__gt__" => DunderGt,
            "__iadd__" => DunderIAdd,
            "__isub__" => DunderISub,
            "__imul__" => DunderIMul,
            "__itruediv__" => DunderITrueDiv,
            "__imod__" => DunderIMod,
            "__ipow__" => DunderIPow,
            "__get__" => DunderGet,
            "__set__" => DunderSet,
            "__getattribute__" => DunderGetAttribute,
            "__getattr__" => DunderGetAttr,
            "__setattr__" => DunderSetAttr,
            _ => throw new ArgumentException($"Unhandled method name: {methodName}"),
        };
    }

    private static bool isDunderMethodName(string methodName) => methodName switch
    {
        "__new__" => true,
        "__call__" => true,
        "__init__" => true,
        "__add__" => true,
        "__sub__" => true,
        "__mul__" => true,
        "__truediv__" => true,
        "__mod__" => true,
        "__pow__" => true,
        "__bool__" => true,
        "__len__" => true,
        "__eq__" => true,
        "__ne__" => true,
        "__lt__" => true,
        "__gt__" => true,
        "__iadd__" => true,
        "__isub__" => true,
        "__imul__" => true,
        "__itruediv__" => true,
        "__imod__" => true,
        "__ipow__" => true,
        "__get__" => true,
        "__set__" => true,
        "__getattribute__" => true,
        "__getattr__" => true,
        "__setattr__" => true,
        _ => false,
    };

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

    internal static EpType EmptyCtor() => new();

    private EpType()
        : base()
    {
    }

    public EpType(string name, EpType[] bases, EpType type)
        : base(type)
    {
        Initialize(name, bases);
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
