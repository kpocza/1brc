using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal class CityMeasurementDictionary : IEnumerable<KeyValuePair<City, Measurement>>
{
    // dictionary must store 10000 items at maximum. next prime in hashhelpers
    private static readonly int SIZE = 10103;
    private static readonly ulong _fastModMultiplier = ulong.MaxValue / (uint)SIZE + 1;

    private int _count;
    private readonly int[] _buckets;
    private readonly Entry[] _entries;
    private readonly City.CityComparer _comparer;

    public CityMeasurementDictionary()
    {
        _buckets = new int[SIZE];
        _entries = new Entry[SIZE];
        _comparer = new City.CityComparer();
        _count = 0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 192)]
    private struct Entry
    {
        public City key; //112
        public uint hashCode; //4
        public int next; //4
        public Measurement value; //16
    }

    public ref Measurement GetValueRefOrAddDefault(City key)
    {
        uint hashCode = (uint)_comparer.GetHashCode(key);

        uint bucketIndex = GetBucketIndex(hashCode); ;
        int bucket = _buckets[bucketIndex];
        int index = bucket - 1;

        while ((uint)index < (uint)_entries.Length)
        {
            ref Entry entry = ref _entries[index];

            if (entry.hashCode == hashCode && _comparer.Equals(entry.key, key))
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
        _buckets[bucketIndex] = index + 1;

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
