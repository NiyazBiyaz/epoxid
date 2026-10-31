namespace Epoxid.Runtime.Objects;

internal static class RuntimeInitializer
{
    internal static EpType Object { get; private set; } = null!;
    internal static EpType Type { get; private set; } = null!;
    internal static EpType SlotDescriptor { get; private set; } = null!;
    internal static EpType Dict { get; private set; } = null!;
    internal static bool IsInitialized { get; private set; } = false;

    internal static void InitializeObjectType()
    {
        Object = EpType.EmptyCtor();
        Type = EpType.EmptyCtor();
        SlotDescriptor = EpType.EmptyCtor();
        Dict = EpType.EmptyCtor();

        Object.DunderClass = Type;
        Type.DunderClass = Type;
        SlotDescriptor.DunderClass = Type;
        Dict.DunderClass = Type;
        IsInitialized = true;

        EpObject.Ready();
        EpType.Ready();
        KnownSlotDescriptor.Ready();
        EpDict.Ready();

        Object.Initialize("object", []);
        Type.Initialize("type", [Object]);
        SlotDescriptor.Initialize("slot descriptor", [Object]);
        Dict.Initialize("dict", [Object]);

        Object.FillSlots(EpObject.KnownSlots);
        Type.FillSlots();
        SlotDescriptor.FillSlots();
        Dict.FillSlots();
    }
}
