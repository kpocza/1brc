struct Measurement
{
    private short _min;
    private short _max;
    private long _sum;
    private int _cnt;

    internal void Apply(short m)
    {
        if (m < _min)
            _min = m;
        if (m > _max)
            _max = m;
        _sum += m;
        _cnt++;
    }

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
        return $"{_min / 10.0:0.0}/{_sum / _cnt / 10.0:0.0}/{_max / 10.0:0.0}";
    }
}
