namespace Epoxid.Runtime.Objects;

public partial class EpObject
{
    public EpType DunderClass { get; internal set; }

    internal Memory<object?> DunderSlots { get; set; }

    /// <summary>
    /// Count of the slots allocated for this class.
    /// </summary>
    internal virtual int SlotsCount { get; } = _EpObjectSlotCount;

    public EpDict? DunderDict
    {
        get => (EpDict?)DunderSlots.Span[dunder_dict_slot_index];
        set => DunderSlots.Span[dunder_dict_slot_index] = value;
    }

    public static readonly EpType Object;

    static EpObject()
    {
        Object = RuntimeInitializer.Object;

        Object.DunderEq = dunderEqImplementation;
        Object.DunderNe = dunderNeImplementation;
        Object.DunderNew = dunderNewImplementation;
        Object.DunderGetAttribute = dunderGetAttributeImplementation;
        Object.DunderSetAttr = dunderSetAttrImplementation;
    }

    private const int dunder_dict_slot_index = 0;

#pragma warning disable IDE1006 // Naming Styles

    protected const int _EpObjectSlotCount = 1;

#pragma warning restore IDE1006 // Naming Styles

    protected EpObject(EpType type)
    {
        allocateSlots();
        DunderClass = type;
    }

    internal EpObject()
    {
        allocateSlots();
        DunderClass = null!;
    }

    private void allocateSlots()
    {
        var slots = new object?[SlotsCount];
        DunderSlots = slots.AsMemory();
    }

    private static EpObject dunderNewImplementation(EpObject type) => new((EpType)type);

    private static EpBool dunderEqImplementation(EpObject self, EpObject other) => (EpBool)self.Equals(other);

    private static EpBool dunderNeImplementation(EpObject self, EpObject other) => (EpBool)!self.Equals(other);

    private static EpObject dunderGetAttributeImplementation(EpObject self, EpObject nameObject)
    {
        if (nameObject is not EpString name)
            // TODO: epoxid runtime error
            throw new ArgumentException($"Cannot return object: given name is not a string: {nameObject}", nameof(nameObject));

        self.DunderClass.DunderDict!.TryGetValue(name, out var fromClassDict);

        if (fromClassDict != null && fromClassDict.DunderClass.DunderGet != null && fromClassDict.DunderClass.DunderSet != null)
        {
            return fromClassDict.DunderClass.DunderGet!(fromClassDict, self, self.DunderClass);
        }

        if (self.DunderDict != null && self.DunderDict.TryGetValue(name, out var fromInstanceDict) && fromInstanceDict != null)
            return fromInstanceDict;

        if (fromClassDict != null && fromClassDict.DunderClass.DunderGet != null)
        {
            return fromClassDict.DunderClass.DunderGet!(fromClassDict, self, self.DunderClass);
        }

        if (self.DunderClass.DunderGetAttr != null)
        {
            return self.DunderClass.DunderGetAttr(self, nameObject);
        }

        throw new Exception($"No attribute '{nameObject}' found."); // TODO: epoxid runtime error AttributeError
    }

    private static void dunderSetAttrImplementation(EpObject self, EpObject nameObject, EpObject value)
    {
        if (nameObject is not EpString name)
            // TODO: epoxid runtime error
            throw new ArgumentException($"Cannot return object: given name is not a string: {nameObject}", nameof(nameObject));

        self.DunderClass.DunderDict!.TryGetValue(name, out var fromClassDict);

        if (fromClassDict != null && fromClassDict.DunderClass.DunderGet != null && fromClassDict.DunderClass.DunderSet != null)
        {
            fromClassDict.DunderClass.DunderSet!(fromClassDict, self, value);
            return;
        }

        if (self.DunderDict != null)
        {
            self.DunderDict[name] = value;
            return;
        }

        // TODO: epoxid runtime error
        throw new InvalidOperationException($"object '{self.DunderClass.DunderName}' has no attribute '{name}' and no __dict__ for setting new attributes");
    }
}
