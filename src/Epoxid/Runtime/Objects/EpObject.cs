using System.Diagnostics;

namespace Epoxid.Runtime.Objects;

public class EpObject
{
    public EpType DunderClass { get; internal set; }

    internal object?[]? DunderSlots { get; set; }

    public EpDict? DunderDict { get; set; }

    public static EpType Object { get; private set; } = null!;

    internal static void Ready()
    {
        Debug.Assert(RuntimeInitializer.IsInitialized);

        Object = RuntimeInitializer.Object;

        Object.DunderEq = dunderEqImplementation;
        Object.DunderNe = dunderNeImplementation;
        Object.DunderNew = dunderNewImplementation;
        Object.DunderGetAttribute = dunderGetAttributeImplementation;
        Object.DunderSetAttr = dunderSetAttrImplementation;
    }

    protected EpObject(EpType type)
    {
        DunderClass = type;
    }

    private protected EpObject()
    {
        DunderClass = null!;
    }

    internal static IReadOnlyList<KnownSlotDescriptor> KnownSlots => [
        new()
        {
            GetSlot = static (_, instance, _) => instance.DunderClass,
            SetSlot = static (_, instance, value) => instance.DunderClass = (EpType)value,
            DescriptorName = "__class__",
        },
        new()
        {
            GetSlot = static (_, instance, _) => instance.DunderDict,
            SetSlot = static (_, instance, value) => instance.DunderDict = (EpDict)value,
            DescriptorName = "__dict__",
        },
    ];

    private static EpObject dunderNewImplementation(EpObject type) => new((EpType)type);

    private static EpBool dunderEqImplementation(EpObject self, EpObject other) => (EpBool)self.Equals(other);

    private static EpBool dunderNeImplementation(EpObject self, EpObject other) => (EpBool)!self.Equals(other);

    private static EpObject? dunderGetAttributeImplementation(EpObject self, EpObject nameObject)
    {
        if (nameObject is not EpString name)
            // TODO: epoxid runtime error
            throw new ArgumentException($"Cannot return object: given name is not a string: {nameObject}", nameof(nameObject));

        var fromClassDict = Core.SearchMethod<EpObject>(self.DunderClass, name);

        if (fromClassDict != null && fromClassDict.DunderClass.DunderGet != null && fromClassDict.DunderClass.DunderSet != null)
        {
            return fromClassDict.DunderClass.DunderGet(fromClassDict, self, self.DunderClass);
        }

        if (self.DunderDict != null && self.DunderDict.TryGetValue(name, out var fromInstanceDict) && fromInstanceDict != null)
            return fromInstanceDict;

        if (fromClassDict != null && fromClassDict.DunderClass.DunderGet != null)
        {
            return fromClassDict.DunderClass.DunderGet(fromClassDict, self, self.DunderClass);
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

        var fromClassDict = Core.SearchMethod<EpObject>(self.DunderClass, name);

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
