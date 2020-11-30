using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using HpTimeStamps.BigMath;
using Xunit;

namespace UnitTests
{
    public class CjmMathUtilFixture
    {
        public ImmutableSortedDictionary<ulong, int> OneCountLookup => OneCountULongs;
        internal ImmutableArray<Int128> ProblematicOperandsForMultiplicationAndDivision => TheProblematicDivMulOperands;
        public ulong RandomULong
        {
            get
            {
                Span<byte> bytes = stackalloc byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                TheRGen.Value.NextBytes(bytes);
                return BitConverter.ToUInt64(bytes);
            }
        }

        public long RandomLong
        {
            get
            {
                Span<byte> bytes = stackalloc byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
                TheRGen.Value.NextBytes(bytes);
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

        internal (Int128 FirstNonProblematicOperand, Int128 SecondNonProblematicOperand) TwoRandomNonProblematicOperands
        {
            get
            {
                bool makeFirst128 = TheRGen.Value.Next(0, 2) == 1;

                Int128 firstOperand = TheProblematicDivMulOperands.First();
                Int128 secondOperand = TheProblematicDivMulOperands.First();
                while (ProblematicOperandsForMultiplicationAndDivision.Contains(firstOperand))
                {
                    firstOperand = makeFirst128 ? RandomInt128 : RandomLong;
                }

                while (ProblematicOperandsForMultiplicationAndDivision.Contains(secondOperand))
                {
                    secondOperand = !makeFirst128 ? RandomInt128 : RandomULong; ;
                }

                return (firstOperand, secondOperand);
            }
        }
        
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
