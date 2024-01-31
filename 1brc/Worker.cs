using System.Runtime.CompilerServices;

unsafe class Worker
{
    private byte* _pointer;
    private long _start;
    private long _end;
    private readonly bool _isClassic;
    private Thread? _thread;
    private CityMeasurementDictionary _measurements;
    
    internal CityMeasurementDictionary Measurements => _measurements;

    internal Worker(byte *pointer, long start, long end, bool isClassic)
    {
        _pointer = pointer;
        _start = start; 
        _end = end;
        _isClassic = isClassic;
        _measurements = new CityMeasurementDictionary();
    }

    internal void Start()
    {
        _thread = new Thread(ProcessChunk);
        _thread.Start();
    }

    internal void Wait()
    {
        _thread!.Join();
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

        if (_isClassic)
        {
            do
            {
                // minlen: 1 byte
                var r = new ReadOnlySpan<byte>(curIdx + 1, 100);
                var cityLength = r.IndexOf(SEP) + 1;

                var city = new City(curIdx, cityLength);

                curIdx += city.Length + 1;

                int m = ParseTemperature(ref curIdx);

                ref var measurement = ref _measurements.GetValueRefOrAddDefaultClassic(in city);
                measurement.Apply((short)m);
            } while (curIdx < localEnd);
        }
        else
        {
            do
            {
                var city = CityBuilder.Create(curIdx);

                curIdx += city.Length + 1;

                int m = ParseTemperature(ref curIdx);

                ref var measurement = ref _measurements.GetValueRefOrAddDefault(in city);
                measurement.Apply((short)m);
            } while (curIdx < localEnd);
#if DEBUG
            Console.WriteLine(_measurements.c * 100.0 / _measurements.a);
#endif
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ParseTemperature(ref byte* curIdx)
    {
        // hacky version of number parsing between -99.9 and 99.9. result is between -999 and 999

        // - sign (ascii 45) or a number (ascii 48-57) can come here. - has no 4th bit set, numbers have.
        // result: 0 for unsigned numbers, 1 for signed numbers
        var signIndicator = 1 - (((*curIdx) & 0x10) >> 4);
        // jump over -
        curIdx += signIndicator;
        // multiplicator: 1 or -1
        int sign = 1 - (signIndicator << 1);

        int num = *(int*)curIdx;

        // num's format is: 32.1 or 2.1. respectively num is 0x01..0203 or 0x0001..02
        // a . character (ascii 46) or a number (ascii 48-57) can be the second byte
        // if second byte's 4th bit is non-zero, then it's at least 10 as no decimal point is there
        int lessThan10NumberIndicator = 1 - ((num & 0x1000) >> 12);
        // shift numbers to common format: 0x01..0203 and 0x01..0200
        num <<= (lessThan10NumberIndicator << 3);
        // eliminate . character: 0x01000203
        num &= 0x0f000f0f;
        // jump over the \n after the number, as well
        curIdx += 5 - lessThan10NumberIndicator;
        //100*0x1000000*0x01000203 + 10*0x10000*0x01000203 + 1*0x01000203 =
        // 0x(100*0x03 + 10*0x02 + 1*0x01)(100*0 + 10*0x03 + 1*0)(100*0 + 10*0 + 1*0x02)(100*0 + 10*0 + 1*0x03)
        //    ~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // 100*0x3... is more than 255 so next bits are also occupied
        // from the 24th bit we have our number multiplied by 10
        return sign * (int)((((long)num * 0x640a0001) >> 24) & 0x3FF);
    }
}
