namespace Epoxid.Runtime.Objects;

// Because we cannot control when the static fields is initialized within a single class, just create a new class
internal static class RuntimeInitializer
{
    internal static EpType Object;
    internal static EpType Type;

    static RuntimeInitializer()
    {
        // We need to avoid using many static dependencies, so use empty ctors and set everything manually
        Object = EpType.EmptyCtor();
        Type = EpType.EmptyCtor();

        Object.DunderSlots = new object?[Object.SlotsCount];
        Object.DunderName = "object";
        Object.DunderClass = Type;
        Object.DunderBases = [];

        Type.DunderSlots = new object?[Type.SlotsCount];
        Type.DunderName = "type";
        Type.DunderClass = Type;
        Type.DunderBases = [Object];

        Object.FillSlots();
        Type.FillSlots();
    }
}
