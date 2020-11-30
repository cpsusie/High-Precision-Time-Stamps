using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using HpTimeStamps.BigMath;
using HpTimeStamps.BigMath.Utils;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class CjmMathTests : OutputHelperAndFixtureHavingTests<CjmMathUtilFixture>
    {
        public CjmMathTests([NotNull] ITestOutputHelper helper, [NotNull] CjmMathUtilFixture fixture) : base(helper, fixture)
        {
        }

        [Fact]
        public void PrintOneSetBitNlzSolution()
        {
            Helper.WriteLine("Going to print number of leading zeros for all 64 bit integers: ");
            foreach (var kvp in Fixture.OneCountLookup)
            {
                Helper.WriteLine("0x{0:X16} has:\t\t{1:N0}\t\tleading zeros.", kvp.Key, kvp.Value);
            }
            Helper.WriteLine("Done printing one count solutions.");
            Helper.WriteLine(string.Empty);
        }

        
        [Fact]
        public void ValidateOneSetBitSolutions()
        {
            KeyValuePair<ulong, int> testKvp = default;
            try
            {
                foreach (var kvp in Fixture.OneCountLookup)
                {
                    testKvp = kvp;
                    Assert.Equal(kvp.Value, CjmUtils.CountLeadingZeroesVerify(kvp.Key));
                    ValidateOneSetCount(kvp.Key);
                }
            }
            catch (Exception)
            {
                Helper.WriteLine("Test failed for [0x{0:x16}]; Expected leading zero count: [{1:N0}]; Actual leading zero count: [{2:N0}].", testKvp.Key, testKvp.Value, CjmUtils.CountLeadingZeroesVerify(testKvp.Key));
                throw;
            }
        }

        [Fact]
        public void DoManyRandomCountLeadingZerosTests()
        {
            ulong testOperand = 0;
            const int numTests = 10_000_000;
            const int updateEvery = 1_000_000;
            int i = 0;
            try
            {
                for (i = 1; i <= numTests; ++i)
                {
                    testOperand = Fixture.RandomULong;
                    if (i % updateEvery == 0)
                        Helper.WriteLine("On test number {0:N0} of {1:N0} tests.\t\t\tTest operand: [0x{2:X16}].", i, numTests, testOperand);
                    
                    ValidateOneSetCount(testOperand);
                }
            }
            catch (Exception)
            {
                Helper.WriteLine("Test {0:N0} of {1:N0} FAILED!  Tested value: [0x{2:X16}].", i, numTests, testOperand);
                throw;
            }

            Helper.WriteLine("All {0:N0} tests PASSED.", numTests);
        }

        [Fact]
        public void DoManyRandomFls128Tests()
        {
            Int128 testOperand = Int128.Zero;
            const int numTests = 10_000_000;
            const int updateEvery = 1_000_000;
            int i = 0;
            try
            {
                for (i = 1; i <= numTests; ++i)
                {
                    testOperand = Fixture.RandomInt128;
                    if (i % updateEvery == 0)
                        Helper.WriteLine("On test number {0:N0} of {1:N0} tests.\t\t\tTest operand: [0x{2:X32}].", i,
                            numTests, testOperand);
                    ValidateFls128(in testOperand);
                }
            }
            catch (Exception)
            {
                Helper.WriteLine("Test {0:N0} of {1:N0} FAILED!  Tested value: [0x{2:X32}].", i, numTests, testOperand);
                throw;
            }
        }

        [Fact]
        public void TestMultiplicationNonEdgeCases()
        {
            const int numTests = 1_000_000;
            const int updateEvery = 100_000;
            int testNo = 0;
            Int128 firstTestOperand=default;
            Int128 secondTestOperand=default;
            Int128 slowResult = 0;
            Int128 fastResult = 0;
            try
            {
                for (testNo = 1; testNo <= numTests; ++testNo)
                {
                    (firstTestOperand, secondTestOperand) = Fixture.TwoRandomNonProblematicOperands;
                    if (testNo % updateEvery == 0)
                        Helper.WriteLine("On test number {0:N0} of {1:N0} tests.\t\t\tLeft Test operand: [0x{2:X32}]\t\t\tRight test operand: [0x{3:X32}].", testNo,
                            numTests, firstTestOperand, secondTestOperand);
                    ValidateNonProblematicMultiplication(in firstTestOperand, in secondTestOperand);
                }
            }
            catch (Exception ex)
            {
                Helper.WriteLine("Test {0:N0} of {1:N0} FAILED!\n", testNo, numTests);
                Helper.WriteLine("Left operand: [0x{0:X32}]\t\t\tRight Operand: [0x{1:X32}]", firstTestOperand, secondTestOperand);
                Helper.WriteLine("Exception message: [{0}].", ex.Message);
                throw;
            }
        }

        [Fact]
        public void TestMultiplicationEdgeCases()
        {
            ReadOnlySpan<Int128> edgeCaseFactors = Fixture.ProblematicOperandsForMultiplicationAndDivision.AsSpan();

            foreach (var item in Permute2Elements(edgeCaseFactors.Length))
            {
                ref readonly Int128 lhs = ref edgeCaseFactors[item.FirstElemIdx];
                ref readonly Int128 rhs = ref edgeCaseFactors[item.SecondElemIdx];
                (ArithmeticException exception, Int128 slowResult, Int128 fastResult) = TestFasterMultiplication(in lhs, in rhs);
                //The code the int128 is based on (originally) is buggy as shit on edge cases.
                Assert.True( 
                    slowResult == fastResult || (ExpectArithmeticException(in lhs, in rhs) && exception != null) ||
                    fastResult == Int128.MinValue || (lhs != rhs &&
                                                      (lhs == Int128.MaxValue || rhs == Int128.MaxValue) &&
                                                      (lhs == -1 || rhs == -1)),
                    $"Edge case multiplication test failed.  Factors {lhs} and {rhs} yielded {slowResult} as the slow result and {fastResult} as the fast result.");
            }

            IEnumerable<(int FirstElemIdx, int SecondElemIdx)> Permute2Elements(int arrayLength)
            {
                for (int arrOneIdx = 0; arrOneIdx < arrayLength; ++arrOneIdx)
                {
                    for (int arrTwoIdx = 0; arrTwoIdx < arrayLength; ++arrTwoIdx)
                    {
                        yield return (arrOneIdx, arrTwoIdx);
                    }
                }
            }
        }

        private static bool ExpectArithmeticException(in Int128 l, in Int128 r) => (l == Int128.MinValue && r == -1) || (r == Int128.MinValue && l == -1);

        [Fact]
        public void DoFls128TestOnRange1To255()
        {
            Int128 testMe = 0;
            while (++testMe < 256)
            {
                ValidateFls128(in testMe);
            }
        }

        private void ValidateNonProblematicMultiplication(in Int128 l, in Int128 r)
        {
            (_, Int128 slowRes, Int128 fastRes) = TestFasterMultiplication(in l, in r);
            Assert.True(slowRes == fastRes, $"Results are not equal.  SlowRes: [0x{slowRes:X32}]\t\t\tFastRes: [0x{fastRes:X32}].");
        }

        private (ArithmeticException Error, Int128 SlowResult, Int128 FastResult) TestFasterMultiplication(in Int128 lhs, in Int128 rhs)
        {

            ArithmeticException ex = null;
            Int128 slow =
                Int128.SlowMultiply(in lhs, in rhs);
            Int128 fast = default;
            try
            {
                fast = CjmUtils.SignedMultiply(in lhs, in rhs);
            }
            catch (ArithmeticException error)
            {
                ex = error;
            }

            return (ex, slow, fast);
        }

        private void ValidateFls128(in Int128 testOperand)
        {
            if (testOperand == 0) return;
            int expected = CjmUtils.Fls128Verify(in testOperand);
            int actual = CjmUtils.Fls128(in testOperand);
            Assert.Equal(expected, actual);
        }

        private void ValidateOneSetCount(ulong value)
        {
            Span<char> span = stackalloc char[64];
            unchecked
            {
                PopulateSpan(span.Slice(0, 32), Convert.ToString((uint)(value >> 32), 2));
                PopulateSpan(span.Slice(32, 32), Convert.ToString((uint)value, 2));
            }
            
            int validatedExpected = CountLeadingZeroesTextually(span);
            int expected = CjmUtils.CountLeadingZeroesVerify(value);
            int actual = value != 0 ? CjmUtils.CountLeadingZeros(value) : 64;
            Assert.Equal(validatedExpected, expected);
            Assert.Equal(expected, actual);
        }

        private void PopulateSpan(Span<char> slice, string toString)
        {
            int stringIdx = toString.Length - 1;
            for (int last = slice.Length - 1; last != -1; --last)
            {
                if (stringIdx > -1 && stringIdx < toString.Length)
                {
                    slice[last] = toString[stringIdx--];
                }
                else
                {
                    slice[last] = '0';
                }
            }
        }


        private int CountLeadingZeroesTextually(ReadOnlySpan<char> span)
        {
            int leadingZeroCount = 0;
            span = span.Trim();
            if (span.Length < 1 || span.Length > 64) throw new ArgumentException("Span must have between 1 and 64 characters, inclusively."); 
            foreach (char c in span)
            {
                switch (c)
                {
                    case '0':
                        ++leadingZeroCount;
                        break;
                    case '1':
                        return leadingZeroCount;
                    default:
                        throw new ArgumentException("parameter must contain nothing but ones and zeroes.");
                }
            }
            Assert.True(false, "This code should Never execute.");
            return leadingZeroCount;
        }

        
    }
}
