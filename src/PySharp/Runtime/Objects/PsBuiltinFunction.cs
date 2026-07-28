namespace PySharp.Runtime.Objects;

public class PsBuiltinFunction : PsObject
{
    public string DunderName { get; }

    private Func<PsObject, PsTuple, PsDict?, PsObject> call { get; }

    public PsBuiltinFunction(string name, Func<PsObject, PsTuple, PsDict?, PsObject> call)
        : base(PsConstants.NativeFunction)
    {
        DunderName = name;
        this.call = call;
    }

    internal static PsObject DunderCallImplementation(PsObject self, PsObject args, PsObject? kwargs)
    {
        if (self is PsBuiltinFunction builtin)
        {
            return builtin.call(self, (PsTuple)args, (PsDict?)kwargs);
        }

        throw new ArgumentException("Object is not a function.", nameof(self));
    }
}
