using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal unsafe static class CityComparer
{
    private static readonly Vector256<byte>[] vectorMasks = new Vector256<byte>[32];

    static CityComparer()
    {
        var bytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        for (int i = 0; i < 32; i++)
        {
            if (i > 0)
                bytes[i - 1] = 255;
            vectorMasks[i] = Vector256.Create(bytes);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Equals(City x, City y)
    {
        var span = new ReadOnlySpan<byte>(x.Start, x.Length);
        var spanOther = new ReadOnlySpan<byte>(y.Start, y.Length);

        return span.SequenceEqual(spanOther);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool EqualsVector(City x, City y)
    {
        if (x.Length != y.Length)
            return false;

        int remaining = x.Length;
        int pos = 0;

        for(int i = 0;i < 3;i++)
        {
            var v1 = Vector256.Load(x.Start + pos);
            var v2 = Vector256.Load(y.Start + pos);

            if(remaining < 32)
            {
                v1 = Vector256.BitwiseAnd(v1, vectorMasks[remaining]);
                v2 = Vector256.BitwiseAnd(v2, vectorMasks[remaining]);
                return v1 == v2;
            }
            if (v1 != v2)
                return false;

            remaining -= 32;
            pos += 32;
        }

        while(remaining > 0)
        {
            if(*(x.Start + pos)!= *(y.Start + pos))
                return false;
            remaining--;
            pos++;
        }

        return true;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetHashCode(City obj)
    {
        if (obj.Length >= 4)
            return (obj.Length * 788761) ^ (int)(*(uint*)obj.Start);

        byte* pos = obj.Start;
        int idx = 0;
        int hash = 0;
        while (idx < obj.Length)
        {
            hash = hash * 31 + (*pos);
            pos++;
            idx++;
        }

        return hash;
    }
}
