namespace PySharp.Runtime.Objects;

// `Native` doesn't mean C, but C# or CLR overall.
public class PsNativeFunction : PsObject
{
    public string DunderName { get; }

    public Func<PsTuple, PsDict, PsObject> Call { get; }

    public PsNativeFunction(string name, Func<PsTuple, PsDict, PsObject> call)
        : base(PsConstants.NativeFunction)
    {
        DunderName = name;
        Call = call;
    }
}
