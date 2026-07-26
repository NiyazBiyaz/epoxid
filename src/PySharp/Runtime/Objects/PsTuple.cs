using System.Collections;

namespace PySharp.Runtime.Objects;

public class PsTuple : PsObject, IReadOnlyCollection<PsObject>
{
    private readonly PsObject[] items;

    public PsTuple(PsObject[] items)
        : base(PsConstants.Tuple)
    {
        this.items = items;
    }

    public int Count => items.Length;

    public IEnumerator<PsObject> GetEnumerator() => ((IEnumerable<PsObject>)items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
