namespace Epoxid.Runtime.Objects;

public class EpBuiltinFunction : EpBaseFunction
{
    public EpBuiltinFunction(string name, FrameCallFunction function)
        : base(EpConstants.NativeFunction, name)
    {
        FrameCall = function;
    }

    public EpBuiltinFunction(string name, FrameCallKeywordFunction function)
        : base(EpConstants.NativeFunction, name)
    {
        FrameKeywordCall = function;
    }

    public FrameCallFunction? FrameCall { get; }

    public FrameCallKeywordFunction? FrameKeywordCall { get; }

    internal readonly static EpType Type = new("native_function", [EpConstants.Object], EpConstants.Type);
}
