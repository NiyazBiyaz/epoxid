namespace Epoxid.Runtime.Objects;

internal static class RuntimeInitializer
{
    internal static EpType Object { get; private set; } = null!;
    internal static EpType Type { get; private set; } = null!;
    internal static bool IsInitialized { get; private set; } = false;

    internal static void InitializeObjectType()
    {
        Object = EpType.EmptyCtor();
        Type = EpType.EmptyCtor();

        Object.DunderClass = Type;
        Type.DunderClass = Type;
        IsInitialized = true;

        EpObject.Ready();
        EpType.Ready();

        Object.Initialize("object", []);
        Type.Initialize("type", [Object]);

        Object.EnrichSlots(EpObject.KnownSlots);
        Type.EnrichSlots();
    }
}
