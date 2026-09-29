namespace Epoxid.Runtime.Objects;

public class EpBuiltinFunction : EpBaseFunction
{
    public EpBuiltinFunction(string name, FrameCallFunction function)
        : base(EpConstants.NativeFunction, name)
    {
        FrameCall = function;
    }

    public FrameCallFunction? FrameCall { get; }

    internal readonly static EpType Type = new("native_function", [EpConstants.Object], EpConstants.Type);
}
