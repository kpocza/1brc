using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

internal unsafe static class CityBuilder
{
    const byte SEP = (byte)';';

    private static readonly Vector256<byte>[] vectorMasks = new Vector256<byte>[32];
    private static readonly Vector256<byte> separator = Vector256.Create((byte)';');
    private static readonly uint[] uintMasks = [0x000000ff, 0x0000ffff, 0x00ffffff, 0xffffffff];

    static CityBuilder()
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
    internal static void Create(byte *curIdx, ref City candidate)
    {
        int length = 0;
        for (int i = 0; i < 3; i++)
        {
            ref Vector256<byte> vect = ref candidate.Vect1;
            if(i == 1)
                vect = ref candidate.Vect2;
            else if(i == 2)
                vect = ref candidate.Vect3;
            
            vect = Vector256.Load(curIdx + length);
            var matches = Vector256.Equals(vect, separator);

            var mask = Vector256.ExtractMostSignificantBits(matches);
            if (mask != 0)
            {
                var semicolonIndex = (uint)BitOperations.TrailingZeroCount(mask);
                vect = Vector256.BitwiseAnd(vect, vectorMasks[semicolonIndex]);

                length += (int)semicolonIndex;
                break;
            }

            length += 32;
        }

        uint tail = 0;
        if (length == 96)
        {
            for (int i = 0; i < 4; i++)
            {
                if (*(curIdx + length) == SEP)
                    break;

                tail = tail * 256 + *(curIdx + length);
                length++;
            }
        }

        candidate.Start = curIdx;
        candidate.Length = length;
        candidate.Tail = tail;
    }
}
