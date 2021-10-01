using System;
using System.Collections.Immutable;
using System.Threading;
using HpTimeStamps.BigMath;
using Xunit;

namespace UnitTests
{
    public class CjmMathUtilFixture
    {
        public long OneDayInMilliseconds = 86_400_000;
        public ImmutableSortedDictionary<ulong, int> OneCountLookup => OneCountULongs;
        internal ImmutableArray<Int128> ProblematicOperandsForMultiplicationAndDivision => TheProblematicDivMulOperands;
        public ulong RandomULong
        {
            get
            {
                Span<byte> bytes = stackalloc byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                RGen.NextBytes(bytes);
                return BitConverter.ToUInt64(bytes);
            }
        }

        internal Random RGen => TheRGen.Value!;

        public bool RandomSign => RGen.Next(0, 2) == 1;

        public long RandomNegativeOneDayToOneDayInMilliseconds
        {
            get
            {
                const ulong oneDayInMilliseconds = 86_400_000ul;
                long ret;
                ulong random = RandomULong;
                unchecked
                {
                    random %= oneDayInMilliseconds;
                    ret = (long) random;
                }
                if (random != 0 && RandomSign)
                {
                    ret = -ret;
                }

                return ret;
            }
        }
        public long RandomLong
        {
            get
            {
                Span<byte> bytes = stackalloc byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                RGen.NextBytes(bytes);
                return BitConverter.ToInt64(bytes);
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

        internal (Int128 FirstNonProblematicOperand, Int128 SecondNonProblematicOperand)
            TwoRandomNonProblematicOperands => (RandomLong, RandomLong);
        
        
        static CjmMathUtilFixture()
        {
           Assert.False(MonotonicStampFixture.StampContext == default);
           
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


        private static readonly ImmutableArray<Int128> TheProblematicDivMulOperands =
            ImmutableArray.Create(Int128.MinValue, Int128.MaxValue, Int128.Zero, 1, -1);
        private static readonly ImmutableSortedDictionary<ulong, int> OneCountULongs = InitOneCountULongs();
        private static readonly ThreadLocal<Random> TheRGen = new ThreadLocal<Random>(() => new Random(), false);
    }
}
