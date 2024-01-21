using System.Runtime.CompilerServices;

internal unsafe static class CityComparer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Equals(ref City x, ref City y)
    {
        var span = new ReadOnlySpan<byte>(x.Start, x.Length);
        var spanOther = new ReadOnlySpan<byte>(y.Start, y.Length);

        return span.SequenceEqual(spanOther);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool EqualsVector(ref City x, ref City y)
    {
        if (x.Length != y.Length)
            return false;

        int l = x.Length;

        if (l <= 32)
            return x.Vect1 == y.Vect1;

        if (l <= 64)
            return x.Vect1 == y.Vect1 && x.Vect2 == y.Vect2;

        if (l <= 96)
            return x.Vect1 == y.Vect1 && x.Vect2 == y.Vect2 && x.Vect3 == y.Vect3;

        return x.Vect1 == y.Vect1 && x.Vect2 == y.Vect2 && x.Vect3 == y.Vect3 && x.Tail == y.Tail;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetHashCode(ref City obj)
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
