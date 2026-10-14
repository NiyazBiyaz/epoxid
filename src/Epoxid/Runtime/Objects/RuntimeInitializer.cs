namespace Epoxid.Runtime.Objects;

// Because we cannot control when the static fields is initialized within a single class, just create a new class
internal static class RuntimeInitializer
{
    internal static EpType Object;
    internal static EpType Type;

    static RuntimeInitializer()
    {
        Object = new("object", []);
        Type = new("type", [Object]);

        Object.DunderClass = Type;
        Type.DunderClass = Type;
    }
}
