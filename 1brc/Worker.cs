using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

unsafe class Worker
{
    private byte* _pointer;
    private long _start;
    private long _end;
    private Thread? _thread;
    private CityMeasurementDictionary _measurements;
    
    internal CityMeasurementDictionary Measurements => _measurements;

    internal Worker(byte *pointer, long start, long end)
    {
        _pointer = pointer;
        _start = start; 
        _end = end;
        _measurements = new CityMeasurementDictionary();
    }

    internal void Start()
    {
        _thread = new Thread(ProcessChunk);
        _thread.Start();
    }

    internal void Wait()
    {
        _thread.Join();
    }

    internal void ProcessChunk()
    {
        const byte NL = 10;
        const byte SEP = (byte)';';

        byte* curIdx = _pointer + _start;
        byte* localEnd = _pointer + _end;

        // if chunk starts in the middle of a row, then go to the next row
        if (_start > 0)
        {
            while ((*(curIdx - 1)) != NL)
                curIdx++;
        }

        // if chunk ends in the middle of a row, then include this row
        while ((*localEnd) != NL)
            localEnd++;

        if(Vector256.IsHardwareAccelerated)
        {
            do
            {
                var city = CityBuilder.Create(curIdx);

                curIdx += city.Length + 1;

                int m = ParseTemperature(ref curIdx);

                ref var measurement = ref _measurements.GetValueRefOrAddDefaultVector(city);
                // default is initialized to full zero. don't need to check bool exist
                measurement.Apply((short)m);
            } while (curIdx < localEnd);
        }
        else
        {
            do
            {
                // minlen: 1 byte
                var r = new ReadOnlySpan<byte>(curIdx + 1, 100);
                var cityLength = r.IndexOf(SEP) + 1;

                var city = new City(curIdx, cityLength);

                curIdx += city.Length + 1;

                int m = ParseTemperature(ref curIdx);

                ref var measurement = ref _measurements.GetValueRefOrAddDefault(city);
                // default is initialized to full zero. don't need to check bool exist
                measurement.Apply((short)m);
            } while (curIdx < localEnd);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ParseTemperature(ref byte* curIdx)
    {
        // hacky version of number parsing between -99.9 and 99.9
        var signIndicator = 1 - (((*curIdx) & 0x10) >> 4);
        curIdx += signIndicator;
        int sign = 1 - (signIndicator << 1);

        int num = *(int*)curIdx;

        int lessThan10NumberIndicator = 1 - ((num & 0x1000) >> 12);
        num <<= (lessThan10NumberIndicator << 3);
        num &= 0x0f000f0f;
        curIdx += 5 - lessThan10NumberIndicator;
        return sign * (int)((((long)num * 0x640a0001) >> 24) & 0x3FF);

/*      const byte DOT = (byte)'.';
        const byte NEG = (byte)'-';
        const byte ZERO = (byte)'0';

        int sign = 1;
        if ((*curIdx) == NEG)
        {
            sign = -1;
            curIdx++;
        }

        int m = 0;
        // handling temperature between -99.9 and 99.9 is enough. Simplify double parsing knowing this fact.
        if ((*(curIdx + 1)) == DOT)
        {
            m = sign * ((*curIdx - ZERO) * 10 + *(curIdx + 2) - ZERO);
            curIdx += 4;
        }
        else
        {
            m = sign * ((*curIdx - ZERO) * 100 + (*(curIdx + 1) - ZERO) * 10 + *(curIdx + 3) - ZERO);
            curIdx += 5;
        }

        return m;*/
    }
}
