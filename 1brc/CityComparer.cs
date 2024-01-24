using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

internal unsafe static class CityComparer
{
    static ReadOnlySpan<byte> VectorMasks => new byte[64]
    {
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    };
    static readonly byte* _vectorMasksMidPtr;

    static CityComparer()
    {
        _vectorMasksMidPtr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(VectorMasks)) + 32;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Equals(in City keyCity, byte* keyData, in City otherCity)
    {
        var span = new ReadOnlySpan<byte>(keyCity.Length <=32 ? keyData : keyCity.Start, keyCity.Length);
        var spanOther = new ReadOnlySpan<byte>(otherCity.Start, otherCity.Length);

        return span.SequenceEqual(spanOther);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool EqualsVector(in City keyCity, byte* keyData, in City otherCity)
    {
        if (keyCity.Length != otherCity.Length)
            return false;

        var len = keyCity.Length;

        if(len <= 32)
        {
            var v1 = Vector256.Load(keyData);
            var v2 = Vector256.Load(otherCity.Start);

            var vectorMask = Vector256.Load(_vectorMasksMidPtr - len);
            return v1 == Vector256.BitwiseAnd(v2, vectorMask);
        }

        return VectorComparerCore(keyCity, otherCity);
    }

    private static bool VectorComparerCore(City keyCity, City otherCity)
    {
        int remaining = keyCity.Length;
        int pos = 0;

        for (int i = 0; i < 3 && remaining > 0; i++)
        {
            var v1 = Vector256.Load(keyCity.Start + pos);
            var v2 = Vector256.Load(otherCity.Start + pos);

            if (remaining < 32)
            {
                var vectorMask = Vector256.Load(_vectorMasksMidPtr - remaining);
                v1 = Vector256.BitwiseAnd(v1, vectorMask);
                v2 = Vector256.BitwiseAnd(v2, vectorMask);

                return v1 == v2;
            }
            if (v1 != v2)
                return false;

            remaining -= 32;
            pos += 32;
        }

        while (remaining > 0)
        {
            if (*(keyCity.Start + pos) != *(otherCity.Start + pos))
                return false;
            remaining--;
            pos++;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetHashCode(in City obj)
    {
        if (obj.Length >= 3)
            return (obj.Length * 788761) ^ (int)(*(uint*)obj.Start);

        return (obj.Length * 788761) ^ (int)(*(ushort*)obj.Start);
    }
}
