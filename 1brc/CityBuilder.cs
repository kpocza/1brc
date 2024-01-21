using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal unsafe static class CityBuilder
{
    const byte SEP = (byte)';';

    private static readonly Vector256<byte> separator = Vector256.Create((byte)';');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static City Create(byte *curIdx)
    {
        int length = 0;

        for (int i = 0; i < 3; i++)
        {
            var vect = Vector256.Load(curIdx + length);
            var matches = Vector256.Equals(vect, separator);

            var mask = Vector256.ExtractMostSignificantBits(matches);
            if (mask != 0)
            {
                length += BitOperations.TrailingZeroCount(mask);
                return new City(curIdx, length);
            }

            length += 32;
        }

        for (int i = 0; i < 4; i++)
        {
            if (*(curIdx + length) == SEP)
                break;
            length++;
        }

        return new City(curIdx, length);
    }
}
