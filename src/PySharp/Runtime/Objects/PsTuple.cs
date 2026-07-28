using System.Collections;

namespace PySharp.Runtime.Objects;

public class PsTuple : PsObject, IReadOnlyList<PsObject>
{
    private readonly PsObject[] items;

    public PsTuple(PsObject[] items)
        : base(PsConstants.Tuple)
    {
        this.items = items;
    }

    public PsObject this[int index] => items[index];

    public int Count => items.Length;

    public IEnumerator<PsObject> GetEnumerator() => ((IEnumerable<PsObject>)items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static readonly PsTuple Empty = new([]);
}
