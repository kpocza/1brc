using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 16)]
struct Measurement
{
    internal short _min;
    private short _max;
    private int _cnt;
    private long _sum;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Apply(short m)
    {
        if (m < _min)
            _min = m;
        if (m > _max)
            _max = m;
        _sum += m;
        _cnt++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Merge(Measurement item)
    {
        if (item._min < _min)
            _min = item._min;
        if (item._max > _max)
            _max = item._max;
        _sum += item._sum;
        _cnt += item._cnt;
    }

    public override string ToString()
    {
        return $"{_min / 10.0:0.0}/{(double)_sum / _cnt / 10.0:0.0}/{_max / 10.0:0.0}";
    }
}
