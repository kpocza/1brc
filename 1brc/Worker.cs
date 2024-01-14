using System.Numerics;
using System.Runtime.InteropServices;

unsafe class Worker
{
    private byte* _pointer;
    private long _start;
    private long _end;
    private Thread? _thread;
    private Dictionary<City, Measurement> _measurements;
    
    internal Dictionary<City, Measurement> Measurements => _measurements;

    internal Worker(byte *pointer, long start, long end)
    {
        _pointer = pointer;
        _start = start; 
        _end = end;
        // comparer is a little bit faster after checking Dictionary's source code
        _measurements = new Dictionary<City, Measurement>(10000, new City.CityComparer());
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
        const byte DOT = (byte)'.';
        const byte NEG = (byte)'-';
        const byte ZERO = (byte)'0';

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

        do
        {
            // https://en.wikipedia.org/wiki/List_of_short_place_names
            // we have only some places with 1 char. Let's ignore them.
            var r = new ReadOnlySpan<byte>(curIdx + 2, 98);
            var cityLength = r.IndexOf(SEP) + 2;

            var city = new City(curIdx, cityLength);

            curIdx += cityLength + 1;

            int sign = 1;
            if ((*curIdx) == NEG)
            {
                sign = -1;
                curIdx++;
            }
            // branchless version of the above (seems not faster)
            //var signIndicator = 1 - (((*curIdx) & 0x10) >> 4);
            //curIdx += signIndicator;
            //int sign = 1 - (signIndicator << 1);

            int m = 0;
            // loop until the second byte is a . ...
            while ((*(curIdx + 1)) != DOT)
            {
                m = m * 10 + *curIdx - ZERO;
                curIdx++;
            }
            // ... since we have exactly 1 fractional digit according to the spec and a.b can be parsed easily
            m = sign * (m * 100 + (*curIdx - ZERO) * 10 + *(curIdx + 2) - ZERO);
            curIdx += 4;

            ref var measurement = ref CollectionsMarshal.GetValueRefOrAddDefault(_measurements, city, out bool exist);
            // default is initialized to full zero. don't need to check bool exist
            measurement.Apply((short)m);
        } while (curIdx < localEnd);
    }
}
