using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

[StructLayout(LayoutKind.Sequential, Size = 112)]
unsafe partial struct City
{
    internal Vector256<byte> Vect1;
    internal Vector256<byte> Vect2;
    internal Vector256<byte> Vect3;
    internal uint Tail;
    internal int Length;
    internal byte* Start;

    internal City(byte* start, int length)
    {
        Start = start;
        Length = length;
    }

    internal City(City candidate)
    {
        Start = candidate.Start;
        Length = candidate.Length;
        Vect1 = candidate.Vect1;
        Vect2 = candidate.Vect2;
        Vect3 = candidate.Vect3;
        Tail = candidate.Tail;
    }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(Start, Length);
    }
}
