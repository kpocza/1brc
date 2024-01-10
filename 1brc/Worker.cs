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
            var r = new ReadOnlySpan<byte>(curIdx, 100);
            var cityLength = r.IndexOf(SEP);

            var city = new City(curIdx, cityLength);

            curIdx += cityLength + 1;

            short sign = 1;
            if ((*curIdx) == NEG)
            {
                sign = -1;
                curIdx++;
            }

            short m = 0;
            // we have exactly 1 fractional digit according to the spec
            while ((*(curIdx + 1)) != DOT)
            {
                m = (short)(m * 10 + *curIdx - ZERO);
                curIdx++;
            }
            m = (short)(sign * (m * 100 + (*curIdx - ZERO) * 10 + *(curIdx + 2) - ZERO));
            curIdx += 4;

            ref var measurement = ref CollectionsMarshal.GetValueRefOrAddDefault(_measurements, city, out bool exist);
            // default is initialized to full zero. don't need to check bool exist
            measurement.Apply(m);
        } while (curIdx < localEnd);
    }
}
