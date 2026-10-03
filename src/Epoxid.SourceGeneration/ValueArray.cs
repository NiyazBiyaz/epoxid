using System.Collections;
using System.Collections.Immutable;

namespace Epoxid.SourceGeneration;

public readonly struct ValueArray<T> : IEnumerable<T>, IEquatable<ValueArray<T>>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> items;

    public readonly int Length
    {
        get
        {
            if (items == default)
                throw new InvalidOperationException("'items' does not contains reference to an array");

            return items.Length;
        }
    }

    public readonly T this[int index]
    {
        get
        {
            if (items == default)
                throw new InvalidOperationException("'items' does not contains reference to an array");

            if (items.Length > index)
            {
                return items[index];
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }
    }

    public ValueArray(ImmutableArray<T> items)
    {
        this.items = items;
    }

    public bool Equals(ValueArray<T> other)
    {
        if (items == default || other.items == default)
            return false;

        if (Length != other.Length)
            return false;

        for (int index = 0; index < Length; index++)
        {
            if (!this[index].Equals(other[index]))
                return false;
        }

        return true;
    }

    public IEnumerator<T> GetEnumerator()
    {
        if (items == default)
            throw new InvalidOperationException("'items' does not contains reference to an array");

        return new Enumerator(this);
    }

    public struct Enumerator(ValueArray<T> values) : IEnumerator<T>
    {
        private readonly ValueArray<T> values = values;
        private int index;

        public readonly T Current => values[index];

        readonly object IEnumerator.Current => Current;

        public readonly void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (index < values.Length)
            {
                index += 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reset() => index = 0;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
