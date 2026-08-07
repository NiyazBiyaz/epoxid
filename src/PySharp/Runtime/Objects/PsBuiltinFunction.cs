namespace PySharp.Runtime.Objects;

public class PsBuiltinFunction(string name, TernaryFunction call)
    : PsBaseFunction(PsConstants.NativeFunction, name)
{
    private TernaryFunction call { get; } = call;

    internal static PsObject DunderCallImplementation(PsObject self, PsObject args, PsObject kwargs)
    {
        if (self is PsBuiltinFunction builtin)
        {
            return builtin.call(self, args, kwargs);
        }

        throw new ArgumentException("Object is not a function.", nameof(self));
    }
}
