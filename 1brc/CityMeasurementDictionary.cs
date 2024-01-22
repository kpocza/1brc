using System.Collections;
using System.Runtime.CompilerServices;

internal class CityMeasurementDictionary : IEnumerable<KeyValuePair<City, Measurement>>
{
    // dictionary must store 10000 items at maximum. next primes in hashhelpers to choose from
    // 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627
    private static readonly int SIZE = 21023;
    private static readonly ulong _fastModMultiplier = ulong.MaxValue / (uint)SIZE + 1;

    private int _count;
    private readonly short[] _buckets;
    private readonly Entry[] _entries;

    public CityMeasurementDictionary()
    {
        _buckets = new short[SIZE];
        _entries = new Entry[SIZE];
        _count = 0;
    }

    internal struct Entry
    {
        internal City key;
        internal uint hashCode;
        internal int next;
        internal Measurement value;
    }

    public ref Measurement GetValueRefOrAddDefault(City key)
    {
        uint hashCode = (uint)CityComparer.GetHashCode(key);

        uint bucketIndex = GetBucketIndex(hashCode);
        int bucket = _buckets[bucketIndex];
        int index = bucket - 1;

        while ((uint)index < (uint)_entries.Length)
        {
            ref Entry entry = ref _entries[index];

            if (entry.hashCode == hashCode && CityComparer.Equals(entry.key, key))
            {
                return ref entry.value!;
            }

            index = entry.next;
        }

        index = _count++;

        ref Entry newEntry = ref _entries[index];
        newEntry.hashCode = hashCode;
        newEntry.next = bucket - 1;
        newEntry.key = key;
        newEntry.value = new Measurement();
        _buckets[bucketIndex] = (short)(index + 1);

        return ref newEntry.value!;
    }

    public ref Measurement GetValueRefOrAddDefaultVector(City key)
    {
        uint hashCode = (uint)CityComparer.GetHashCode(key);

        uint bucketIndex = GetBucketIndex(hashCode); ;
        int bucket = _buckets[bucketIndex];
        int index = bucket - 1;

        while ((uint)index < (uint)_entries.Length)
        {
            ref Entry entry = ref _entries[index];

            if (entry.hashCode == hashCode && CityComparer.EqualsVector(entry.key, key))
            {
                return ref entry.value!;
            }

            index = entry.next;
        }

        index = _count++;

        ref Entry newEntry = ref _entries[index];
        newEntry.hashCode = hashCode;
        newEntry.next = bucket - 1;
        newEntry.key = key;
        newEntry.value = new Measurement();

        _buckets[bucketIndex] = (short)(index + 1);

        return ref newEntry.value!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetBucketIndex(uint value) => (uint)(((((_fastModMultiplier * value) >> 32) + 1) * (uint)SIZE) >> 32);

    IEnumerator<KeyValuePair<City, Measurement>> IEnumerable<KeyValuePair<City, Measurement>>.GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            var entry = _entries[i];
            yield return new KeyValuePair<City, Measurement>(entry.key, entry.value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<City, Measurement>>)this).GetEnumerator();
}
