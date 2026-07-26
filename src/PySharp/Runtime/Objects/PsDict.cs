using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace PySharp.Runtime.Objects;

public class PsDict : PsObject, IDictionary<PsObject, PsObject>
{
    private readonly Dictionary<PsObject, PsObject> items;

    public PsDict(Dictionary<PsObject, PsObject> items)
        : base(PsConstants.Dict)
    {
        this.items = items;
    }

    public PsObject this[PsObject key]
    {
        get => items[key];
        set => items[key] = value;
    }

    public ICollection<PsObject> Keys => items.Keys;

    public ICollection<PsObject> Values => items.Values;

    public int Count => items.Count;

    public bool IsReadOnly => false;

    public void Add(PsObject key, PsObject value) => items.Add(key, value);

    public void Add(KeyValuePair<PsObject, PsObject> item) => Add(item.Key, item.Value);

    public void Clear() => items.Clear();

    public bool Contains(KeyValuePair<PsObject, PsObject> item) => items.Contains(item);

    public bool ContainsKey(PsObject key) => items.ContainsKey(key);

    public void CopyTo(KeyValuePair<PsObject, PsObject>[] array, int arrayIndex) => throw new NotImplementedException();

    public IEnumerator<KeyValuePair<PsObject, PsObject>> GetEnumerator() => items.GetEnumerator();

    public bool Remove(PsObject key) => items.Remove(key);

    public bool Remove(KeyValuePair<PsObject, PsObject> item) => items.Remove(item.Key);

    public bool TryGetValue(PsObject key, [MaybeNullWhen(false)] out PsObject value) => items.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
