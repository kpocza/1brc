using System.Runtime.CompilerServices;
using System.Text;

unsafe struct City
{
    private readonly byte* _start;
    private readonly int _length;

    public City(byte* start, int length)
    {
        _start = start;
        _length = length;
    }

    internal class CityComparer : IEqualityComparer<City>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(City x, City y)
        {
            var span = new ReadOnlySpan<byte>(x._start, x._length);
            var spanOther = new ReadOnlySpan<byte>(y._start, y._length);

            return span.SequenceEqual(spanOther);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(City obj)
        {
            if (obj._length >= 4)
                return (obj._length * 788761) ^ (int)(*(uint*)obj._start);

            byte* pos = obj._start;
            int idx = 0;
            int hash = 0;
            while (idx < obj._length)
            {
                hash = hash * 31 + (*pos);
                pos++;
                idx++;
            }

            return hash;
        }
    }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(_start, _length);
    }
}
