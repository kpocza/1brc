using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal unsafe static class CityBuilder
{
    const byte SEP = (byte)';';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static City Create(byte *curIdx)
    {
        var separator = Vector256.Create(SEP);
        int length = 0;

        for (length = 0; length < 96; length+=32)
        {
            var vect = Vector256.Load(curIdx + length);
            var matches = Vector256.Equals(vect, separator);

            var mask = Vector256.ExtractMostSignificantBits(matches);
            if (mask != 0)
            {
                length += BitOperations.TrailingZeroCount(mask);

                return new City(curIdx, length);
            }
        }

        while (length < 100 && *(curIdx + length) != SEP)
        {
            length++;
        }

        return new City(curIdx, length);
    }
}
