namespace Epoxid.Runtime.Objects;

public class EpBuiltinFunction : EpBaseFunction
{
    public EpBuiltinFunction(string name, FrameCallFunction function)
        : base(EpConstants.NativeFunction, name)
    {
        FrameCall = function;
    }

    public EpBuiltinFunction(string name, FrameKeywordCallFunction function)
        : base(EpConstants.NativeFunction, name)
    {
        FrameKeywordCall = function;
    }

    public FrameCallFunction? FrameCall { get; }

    public FrameKeywordCallFunction? FrameKeywordCall { get; }
}
