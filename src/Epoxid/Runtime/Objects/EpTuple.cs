using System.Collections;
using System.Runtime.CompilerServices;

namespace Epoxid.Runtime.Objects;

[CollectionBuilder(typeof(EpTupleBuilder), "Create")]
public class EpTuple : EpObject, IReadOnlyList<EpObject>
{
    private readonly EpObject[] items;

    public EpTuple(EpObject[] items)
        : base(EpConstants.Tuple)
    {
        this.items = items;
    }

    public EpTuple(ReadOnlySpan<EpObject> items)
        : base(EpConstants.Tuple)
    {
        this.items = [.. items];
    }

    public EpObject this[int index] => items[index];

    internal static readonly EpType Type = new("tuple", [EpConstants.Object], EpConstants.Type)
    {
        DunderLen = DunderLenImplementation,
    };

    public int Count => items.Length;

    public IEnumerator<EpObject> GetEnumerator() => ((IEnumerable<EpObject>)items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static readonly EpTuple Empty = new([]);

    internal static EpInteger DunderLenImplementation(EpObject self) => (EpInteger)((EpTuple)self).items.Length;
}

public static class EpTupleBuilder
{
    public static EpTuple Create(ReadOnlySpan<EpObject> values) => new(values);
}
