using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Tests
{
    [TestClass]
    public unsafe class NumberTests
    {
        private int ParseNumber(byte[] numbers)
        {
            byte* curIdx = (byte *)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(numbers));

            var signIndicator = 1 - (((*curIdx) & 0x10) >> 4);
            curIdx += signIndicator;
            int sign = 1 - (signIndicator << 1);

            int num = *(int*)curIdx;

            int dotIndicator = 1 - ((num&0x1000) >> 12);
            num <<= (dotIndicator << 3);
            num &= 0x0f000f0f;
            return sign * (int)((((long)num * 0x640a0001) >> 24) & 0x3FF);
        }

        private byte[] GetBytes(string val)
        {
            return System.Text.Encoding.ASCII.GetBytes(val);
        }

        [TestMethod]
        public void Parse12()
        {
            Assert.AreEqual(12, ParseNumber(GetBytes("1.2\0")));
        }

        [TestMethod]
        public void Parse123()
        {
            Assert.AreEqual(123, ParseNumber(GetBytes("12.3\0")));
        }

        [TestMethod]
        public void ParseM12()
        {
            Assert.AreEqual(-12, ParseNumber(GetBytes("-1.2\0")));
        }

        [TestMethod]
        public void ParseM123()
        {
            Assert.AreEqual(-123, ParseNumber(GetBytes("-12.3\0")));
        }

        [TestMethod]
        public void Parse99()
        {
            Assert.AreEqual(99, ParseNumber(GetBytes("9.9\0")));
        }

        [TestMethod]
        public void Parse999()
        {
            Assert.AreEqual(999, ParseNumber(GetBytes("99.9\0")));
        }

        [TestMethod]
        public void Parse05()
        {
            Assert.AreEqual(05, ParseNumber(GetBytes("0.5\0")));
        }
    }
}