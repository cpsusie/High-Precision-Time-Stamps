using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using HpTimeStamps.BigMath;
using Xunit;

namespace UnitTests
{
    public class CjmMathUtilFixture
    {
        public ImmutableSortedDictionary<ulong, int> OneCountLookup => OneCountULongs;

        public ulong RandomULong
        {
            get
            {
                Span<byte> bytes = stackalloc byte[8]{0, 0, 0 ,0 ,0 , 0 , 0 , 0 };
                TheRGen.Value.NextBytes(bytes);
                return BitConverter.ToUInt64(bytes);
            }
        }

        internal Int128 RandomInt128
        {
            get
            {
                ulong low = RandomULong;
                ulong high = RandomULong;
                return new Int128(high, low);
            }
        }
        
        static CjmMathUtilFixture()
        {
            Assert.True((new MonotonicStampFixture()).Now != DateTime.MinValue);
        }

        private static ImmutableSortedDictionary<ulong, int> InitOneCountULongs()
        {
            var bldr = ImmutableSortedDictionary.CreateBuilder<ulong, int>();
            int leadingZeroes = 63;
            ulong value = 0x0000_0000_0000_0001;
            do
            {
                bldr.Add(value, leadingZeroes);
                value <<= 1;
                --leadingZeroes;
            } while (value != 0);

            return bldr.ToImmutable();
        }

        private static readonly ImmutableSortedDictionary<ulong, int> OneCountULongs = InitOneCountULongs();
        private static readonly ThreadLocal<Random> TheRGen = new ThreadLocal<Random>(() => new Random(), false);
    }
}
