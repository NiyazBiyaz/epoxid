using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Epoxid.Runtime.Objects;

public class EpDict : EpObject, IDictionary<EpObject, EpObject>
{
    private readonly Dictionary<EpObject, EpObject> items;

    public EpDict(Dictionary<EpObject, EpObject> items)
        : base(EpConstants.Dict)
    {
        this.items = items;
    }

    public EpDict()
        : base(EpConstants.Dict)
    {
        items = [];
    }

    public EpObject this[EpObject key]
    {
        get => items[key];
        set => items[key] = value;
    }

    public static readonly EpDict Empty = [];

    public ICollection<EpObject> Keys => items.Keys;

    public ICollection<EpObject> Values => items.Values;

    public int Count => items.Count;

    public bool IsReadOnly => false;

    public void Add(EpObject key, EpObject value) => items.Add(key, value);

    public void Add(KeyValuePair<EpObject, EpObject> item) => Add(item.Key, item.Value);

    public void Clear() => items.Clear();

    public bool Contains(KeyValuePair<EpObject, EpObject> item) => items.Contains(item);

    public bool ContainsKey(EpObject key) => items.ContainsKey(key);

    public void CopyTo(KeyValuePair<EpObject, EpObject>[] array, int arrayIndex) => throw new NotImplementedException();

    public IEnumerator<KeyValuePair<EpObject, EpObject>> GetEnumerator() => items.GetEnumerator();

    public bool Remove(EpObject key) => items.Remove(key);

    public bool Remove(KeyValuePair<EpObject, EpObject> item) => items.Remove(item.Key);

    public bool TryGetValue(EpObject key, [MaybeNullWhen(false)] out EpObject value) => items.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal static EpInteger DunderLenImplementation(EpObject self) => (EpInteger)((EpDict)self).items.Count;
}
