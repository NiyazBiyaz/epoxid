using System.Collections;
using System.Runtime.CompilerServices;

namespace PySharp.Runtime.Objects;

[CollectionBuilder(typeof(PsTupleBuilder), "Create")]
public class PsTuple : PsObject, IReadOnlyList<PsObject>
{
    private readonly PsObject[] items;

    public PsTuple(PsObject[] items)
        : base(PsConstants.Tuple)
    {
        this.items = items;
    }

    public PsTuple(ReadOnlySpan<PsObject> items)
        : base(PsConstants.Tuple)
    {
        this.items = [.. items];
    }

    public PsObject this[int index] => items[index];

    public int Count => items.Length;

    public IEnumerator<PsObject> GetEnumerator() => ((IEnumerable<PsObject>)items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static readonly PsTuple Empty = new([]);

    internal static PsInteger DunderLenImplementation(PsObject self) => (PsInteger)((PsTuple)self).items.Length;
}

public static class PsTupleBuilder
{
    public static PsTuple Create(ReadOnlySpan<PsObject> values) => new(values);
}
