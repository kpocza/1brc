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
    internal static bool EqualsClassic(in City keyCity, byte* keyData, in City otherCity)
    {
        var span = new ReadOnlySpan<byte>(keyCity.Length <= 32 ? keyData : keyCity.Start, keyCity.Length);
        var spanOther = new ReadOnlySpan<byte>(otherCity.Start, otherCity.Length);

        return span.SequenceEqual(spanOther);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool EqualsShort(byte* keyData, in City otherCity)
    {
        var v1 = Vector256.Load(keyData);
        var v2 = Vector256.Load(otherCity.Start);

        var vectorMask = Vector256.Load(_vectorMasksMidPtr - otherCity.Length);

        return v1 == Vector256.BitwiseAnd(v2, vectorMask);
    }

    internal static bool EqualsLong(in City keyCity, in City otherCity)
    {
        var v1 = Vector256.Load(keyCity.Start);
        var v2 = Vector256.Load(otherCity.Start);
        if (v1 != v2)
            return false;

        int remaining = keyCity.Length - 32;
        int pos = 32;

        for (; pos < 96 && remaining > 0; pos += 32)
        {
            v1 = Vector256.Load(keyCity.Start + pos);
            v2 = Vector256.Load(otherCity.Start + pos);

            if (remaining < 32)
            {
                var vectorMask = Vector256.Load(_vectorMasksMidPtr - remaining);

                return Vector256.BitwiseAnd(v1, vectorMask) == Vector256.BitwiseAnd(v2, vectorMask);
            }
            if (v1 != v2)
                return false;

            remaining -= 32;
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
        return (obj.Length * 788761) ^ (obj.Length >= 3 ? (int)(*(uint*)obj.Start) : (int)(*(ushort*)obj.Start));
    }
}
