using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

[StructLayout(LayoutKind.Sequential, Size = 112)]
unsafe struct City
{
    private readonly Vector256<byte> part1;
    private readonly Vector256<byte> part2;
    private readonly Vector256<byte> part3;
    private readonly uint part4;
    private readonly byte* _start;
    private readonly int _length;

    private static readonly Vector256<byte>[] vectorMasks = new Vector256<byte>[32];
    private static readonly uint[] uintMasks = [0x000000ff, 0x0000ffff, 0x00ffffff, 0xffffffff];

    static City()
    {
        var bytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        for (int i = 0; i < 32; i++)
        {
            bytes[i] = 255;
            vectorMasks[i] = Vector256.Create(bytes);
        }
    }

    public City(byte* start, int length)
    {
        _start = start;
        _length = length;

        int l = length;

        if (l >= 32)
        {
            part1 = Vector256.Load(_start);
        }
        else
        {
            part1 = Vector256.BitwiseAnd(Vector256.Load(_start), vectorMasks[l - 1]);
            return;
        }

        l -= 32;
        if (l <= 0)
            return;

        if (l >= 32)
        {
            part2 = Vector256.Load(_start + 32);
        }
        else
        {
            part2 = Vector256.BitwiseAnd(Vector256.Load(_start + 32), vectorMasks[l - 1]);
            return;
        }

        l -= 32;
        if (l <= 0)
            return;

        if (l >= 32)
        {
            part3 = Vector256.Load(_start + 64);
        }
        else
        {
            part3 = Vector256.BitwiseAnd(Vector256.Load(_start + 64), vectorMasks[l - 1]);
            return;
        }
        l -= 32;
        if (l <= 0)
            return;

        part4 = (*(uint*)(_start + 96)) & uintMasks[l - 1];
    }

    internal class CityComparer : IEqualityComparer<City>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(City x, City y)
        {
            //            var span = new ReadOnlySpan<byte>(x._start, x._length);
            //          var spanOther = new ReadOnlySpan<byte>(y._start, y._length);

            //        return span.SequenceEqual(spanOther);
            if(x._length!= y._length) 
                return false;

            int l = x._length;

            if(l <=32)
                return x.part1 == y.part1;

            if(l <= 64)
                return x.part1 == y.part1 && x.part2 == y.part2;

            if (l <= 96)
                return x.part1 == y.part1 && x.part2 == y.part2 && x.part3 == y.part3;

            return x.part1 == y.part1 && x.part2 == y.part2 && x.part3 == y.part3 && x.part4 == y.part4;
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
