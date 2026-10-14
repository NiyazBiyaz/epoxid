namespace Epoxid.Runtime.Objects;

public class EpBuiltinFunction : EpBaseFunction
{
    public EpBuiltinFunction(string name, FrameCallFunction function)
        : base(BuiltinFunction, name)
    {
        FrameCall = function;
    }

    public FrameCallFunction? FrameCall { get; }

    internal readonly static EpType BuiltinFunction = new("native_function", [Object], EpType.Type);
}
