using System.Runtime.CompilerServices;
using System.Text;

unsafe struct City
{
    internal byte* Start;
    internal int Length;

    internal City(byte* start, int length)
    {
        Start = start;
        Length = length;
    }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(Start, Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void CopyTo(byte *target)
    {
        new ReadOnlySpan<byte>(Start, Length).CopyTo(new Span<byte>(target, Length));
    }
}
