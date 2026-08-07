namespace PySharp.Runtime.Objects;

public class PsFunction(string name) : PsBaseFunction(PsConstants.Function, name)
{
    /* Here should be function implementation, but not now. */

    internal static PsObject DunderCallImplementation(PsObject self, PsObject args, PsObject kwargs)
    {
        if (self is PsFunction userFunc)
        {
            throw new NotImplementedException("TODO: return Interpreter.InterpretFunctionCall(userFunc, args, kwargs);");
        }

        throw new ArgumentException("Object is not a function.", nameof(self));
    }
}
