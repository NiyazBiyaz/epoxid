namespace PySharp.Runtime.Objects;

public class PsBuiltinFunction : PsBaseFunction
{
    public PsBuiltinFunction(string name, FrameCallFunction function)
        : base(PsConstants.NativeFunction, name)
    {
        FrameCall = function;
    }

    public PsBuiltinFunction(string name, FrameKeywordCallFunction function)
        : base(PsConstants.NativeFunction, name)
    {
        FrameKeywordCall = function;
    }

    public FrameCallFunction? FrameCall { get; }

    public FrameKeywordCallFunction? FrameKeywordCall { get; }
}
