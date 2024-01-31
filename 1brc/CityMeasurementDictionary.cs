using System.Collections;
using System.Runtime.CompilerServices;

internal unsafe class CityMeasurementDictionary : IEnumerable<KeyValuePair<City, Measurement>>
{
    // dictionary must store 10000 items at maximum. next primes in hashhelpers to choose from
    // 10103, 12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627
    private static readonly int SIZE = 10010;
    private static readonly int BUCKETCOUNT = 43627;
    private static readonly ulong _fastModMultiplier = ulong.MaxValue / (uint)BUCKETCOUNT + 1;

    private int _count;
    private readonly short[] _buckets;
    private readonly Entry[] _entries;
    private readonly byte[] _localCities;
    private readonly short* _bucketsHead;
    private readonly Entry* _entriesHead;
    private readonly byte* _localCitiesHead;

    public CityMeasurementDictionary()
    {
        _buckets = GC.AllocateArray<short>(BUCKETCOUNT, true);
        _entries = GC.AllocateArray<Entry>(SIZE, true);
        _localCities = GC.AllocateArray<byte>(SIZE*32, true);
        _count = 0;

        _bucketsHead = (short*)Unsafe.AsPointer(ref _buckets[0]);
        _entriesHead = (Entry*)Unsafe.AsPointer(ref _entries[0]);
        _localCitiesHead = (byte*)Unsafe.AsPointer(ref _localCities[0]);
    }

    internal struct Entry
    {
        internal City key;
        internal Measurement value;
        internal uint hashCode;
        internal int next;
    }

    public ref Measurement GetValueRefOrAddDefaultClassic(in City key)
    {
        uint hashCode = (uint)CityComparer.GetHashCode(in key);

        uint bucketIndex = GetBucketIndex(hashCode);
        int bucket = *(_bucketsHead + bucketIndex);
        int index = bucket - 1;

        while ((uint)index < (uint)_entries.Length)
        {
            ref Entry entry = ref *(_entriesHead + index);

            if (entry.hashCode == hashCode && CityComparer.EqualsClassic(in entry.key, _localCitiesHead + index * 32, in key))
            {
                return ref entry.value!;
            }

            index = entry.next;
        }

        index = _count++;

        ref Entry newEntry = ref *(_entriesHead + index);
        newEntry.hashCode = hashCode;
        newEntry.next = bucket - 1;
        newEntry.key = key;
        newEntry.value = new Measurement();
        *(_bucketsHead + bucketIndex) = (short)(index + 1);

        if (key.Length <= 32)
            key.CopyTo(_localCitiesHead + index * 32);

        return ref newEntry.value!;
    }

#if DEBUG
    public int c = 0;
    public int a = 0;
#endif
    public ref Measurement GetValueRefOrAddDefault(in City key)
    {
        uint hashCode = (uint)CityComparer.GetHashCode(in key);

        uint bucketIndex = GetBucketIndex(hashCode);
        int bucket = *(_bucketsHead + bucketIndex);
        int index = bucket - 1;

        if (key.Length < 32)
        {
            while ((uint)index < (uint)_entries.Length)
            {
                ref Entry entry = ref *(_entriesHead + index);
#if DEBUG
                a++;
#endif
                // don't need to check hash and length just compare at most 31 bytes of data
                // bytes after the length of the city (max 31) are set to \0 which substitutes length comparison
                // utf-8 characters cannot have 0 as the last byte, and the test set doesn't allow \0 character in the name of the city
                if (CityComparer.EqualsShort(_localCitiesHead + index * 32, in key))
                {
                    return ref entry.value!;
                }
#if DEBUG
                c++;
#endif

                index = entry.next;
            }
        }
        else
        {
            while ((uint)index < (uint)_entries.Length)
            {
                ref Entry entry = ref *(_entriesHead + index);

                if (entry.hashCode == hashCode && CityComparer.EqualsLong(in entry.key, in key))
                {
                    return ref entry.value!;
                }

                index = entry.next;
            }
        }

        index = _count++;

        ref Entry newEntry = ref *(_entriesHead + index);
        newEntry.hashCode = hashCode;
        newEntry.next = bucket - 1;
        newEntry.key = key;
        newEntry.value = new Measurement();
        *(_bucketsHead + bucketIndex) = (short)(index + 1);

        if (key.Length < 32)
            key.CopyTo(_localCitiesHead + index * 32);

        return ref newEntry.value!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetBucketIndex(uint value) => (uint)(((((_fastModMultiplier * value) >> 32) + 1) * (uint)BUCKETCOUNT) >> 32);

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
