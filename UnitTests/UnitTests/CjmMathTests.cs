using System;
using System.Collections.Generic;
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

        [Fact]
        public void TestMinValueConversions()
        {
            Int128 minValue = Int128.MinValue;
            Int128 minValuePlusOne = minValue + 1;
            double dlbPlusOne = (double) minValuePlusOne;
            decimal dedPlusOne = (decimal) minValuePlusOne;
            UInt128 unsigned = (UInt128) minValuePlusOne;

            double dblMin = (double) minValue;
            decimal decMin = (decimal) minValue;
            UInt128 uMin = (UInt128) minValue;
                
            Helper.WriteLine("Min value + 1 conversions worked ok.  double: {0}, decimal{1}, uint128: {2}.", dlbPlusOne,
                dedPlusOne, unsigned);
            Helper.WriteLine("Min value conversions worked ok.  double: {0}, decimal{1}, uint128: {2}.", dblMin,
                decMin, uMin);
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

        [Fact]
        public void ConversionTestCaseOne()
        {
            const long rawMs = 49_799_470;
            const long tsTicksPerSecond = 10_000_000;
            const long durationTicksPerSecond = 2_441_465;
            Assert.Equal(tsTicksPerSecond, TimeSpan.TicksPerSecond);

            TestConversionArithmetic(rawMs, durationTicksPerSecond, tsTicksPerSecond);
        }

        [Fact]
        public void TestNegativeNarrowingCast()
        {
            Int128 negativeSixBillion = -6_000_000_000;
            long narrowed = (long) negativeSixBillion;
            Assert.True(narrowed == negativeSixBillion);
        }
        
        [Fact]
        public void TestPrintWithSeparators()
        {
            Int128 printMe = new Int128(0xc0de_d00d_fea2_b00b, 0xcafe_babe_face_b00b);
            string withoutSeparators = printMe.ToString();
            string asHexaDecimal = printMe.ToString("X");
            string withSeparators = printMe.ToString("N");
            Helper.WriteLine("decimal: {0}.", withoutSeparators);
            Helper.WriteLine("hexadecimal: 0x{0}.", asHexaDecimal);
            Helper.WriteLine("With separators: {0}.", withSeparators);
            Assert.Equal("-83,913,681,977,670,342,067,983,590,708,596,527,093", withSeparators);
            Int128 zero = 0;
            Int128 negOne = -1;
            Int128 one = 1;
            Int128 ten = 10;
            Int128 negTen = -10;
            Int128 ninetyNine = 99;
            Int128 negativeNinetyNine = -99;
            Int128 oneHundred = 100;
            Int128 negOneHundred = -100;
            Int128 ninehundredNinetyNine = 999;
            Int128 negNinehundredNinetyNine = -999;
            Int128 oneThousand = 1000;
            Int128 negOneThousand = -1000;
            Int128 oneHundredThousand = 100000;
            Int128 negOneHundredThousand = -100000;
            Assert.Equal("0",zero.ToString("n"));
            Assert.Equal("-1", negOne.ToString("N"));
            Assert.Equal("1", one.ToString("N"));
            Assert.Equal("10", ten.ToString("N"));
            Assert.Equal("-10", negTen.ToString("N"));
            Assert.Equal("99", ninetyNine.ToString("N"));
            Assert.Equal("-99", negativeNinetyNine.ToString("N"));
            Assert.Equal("100", oneHundred.ToString("N"));
            Assert.Equal("-100", negOneHundred.ToString("N"));
            Assert.Equal("999", ninehundredNinetyNine.ToString("N"));
            Assert.Equal("-999", negNinehundredNinetyNine.ToString("N"));
            Assert.Equal("1,000", oneThousand.ToString("N"));
            Assert.Equal("-1,000", negOneThousand.ToString("N"));
            Assert.Equal("100,000", oneHundredThousand.ToString("N"));
            Assert.Equal("-100,000", negOneHundredThousand.ToString("N"));
        }
        
        [Fact]
        public void TestRandomConversions()
        {
            const int numTests = 10_000_000;
            const long tsTicksPerSecond = 10_000_000;
            const long durationTicksPerSecond = 2_441_465;
            Assert.Equal(tsTicksPerSecond, TimeSpan.TicksPerSecond);

            for (int testNo = 1; testNo <= numTests; ++testNo)
            {
                if (testNo % 100_000 == 0)
                {
                    Helper.WriteLine($"On test {testNo:N0} of {numTests:N0}.");
                }
                long rawMs = Fixture.RandomNegativeOneDayToOneDayInMilliseconds;
                Assert.True(rawMs >= -Fixture.OneDayInMilliseconds && rawMs <= Fixture.OneDayInMilliseconds);
                try
                {
                    TestConversionArithmetic(rawMs, durationTicksPerSecond, tsTicksPerSecond);
                }
                catch (Exception e)
                {
                    Helper.WriteLine($"Test failed.  Value: {rawMs:N0}; Exception: {e}.");
                    throw;
                }
            }
            
        }
       
        
        private void TestConversionArithmetic(long rawMilliseconds, long stopwatchTicksPerSecond, long timespanTicksPerSecond)
        {
            //Helper.WriteLine($"Raw milliseconds: {rawMilliseconds:N0}.");
            //Helper.WriteLine($"Stopwatch Ticks / Second: {stopwatchTicksPerSecond:N0}.");
            //Helper.WriteLine($"Timespan Ticks Per Second: {timespanTicksPerSecond:N0}.");
            Int128 gcd = (long) Gcd((ulong) stopwatchTicksPerSecond, (ulong) timespanTicksPerSecond);
            (Int128 stopWatchTicksReduced, Int128 swtRemainder) = Int128.DivRem(stopwatchTicksPerSecond, gcd);
            Assert.True(swtRemainder == 0);
            (Int128 timespanTicksReduced, Int128 tstRemainder) = Int128.DivRem(timespanTicksPerSecond, gcd);
            Assert.True(tstRemainder == 0);

            TimeSpan ts = TimeSpan.FromMilliseconds(rawMilliseconds);
            Assert.True((long) ts.TotalMilliseconds == rawMilliseconds);


            Int128 timeSpanTicks = ts.Ticks;
            Int128 durationTicks = ConvertTsTicksToSwTicks(timeSpanTicks);
            //Helper.WriteLine("Stopwatch ticks: {0}.", durationTicks );
            //Helper.WriteLine("Timespan ticks: {0}.", timeSpanTicks);
            Assert.True(durationTicks >= long.MinValue && durationTicks <= long.MaxValue);
              

            (double dtMsWithFrac, long durationTickMilliseconds) = ConvertDurationTicksToMilliseconds((long) durationTicks);
            //Helper.WriteLine("Duration milliseconds.  Integer: {0:N0}; Float: {1:N}.", durationTickMilliseconds, dtMsWithFrac);
            //Helper.WriteLine("Time span milliseconds: {0:N}", ts.TotalMilliseconds);
            double diff = Math.Abs(dtMsWithFrac - ts.TotalMilliseconds);
            //Helper.WriteLine($"Difference: {diff}.");
            Assert.True(diff <= 1.5, $"milliseconds difference must be less than 1.5.  Actual value: {diff:N}.");

            Int128 timespanTicksRoundTripped = ConvertSwTicksToTsTicks(in durationTicks);
            Assert.True(timespanTicksRoundTripped <= long.MaxValue && timespanTicksRoundTripped >= long.MinValue);
            ref readonly Int128 bigger = ref timeSpanTicks;
            ref readonly Int128 smaller = ref timespanTicksRoundTripped;
            if (bigger < smaller)
            {
                bigger = ref timespanTicksRoundTripped;
                smaller = ref timeSpanTicks;
            }

            long tickDiff = (long) (bigger - smaller);
            TimeSpan ticksDiff = TimeSpan.FromTicks(tickDiff);
            //Helper.WriteLine($"difference between original and round tripped stopwatch ticks: [{tickDiff:N0}], or {ticksDiff.TotalMilliseconds:N3}." );
            Assert.True(ticksDiff.TotalMilliseconds <= 1.5, $"ticksDiff > 1.5 milliseconds. value: {ticksDiff}.");


            (double Float, long Integer) ConvertDurationTicksToMilliseconds(long dtks)
            {
                const long toMsConvFactor = 1_000;
                Int128 dt = dtks;
                Int128 intermediate = dtks * toMsConvFactor;
                (Int128 resultQ, Int128 resultR) = Int128.DivRem(intermediate, stopwatchTicksPerSecond);
                double fraction = resultR == 0 ? 0 : (double) resultR / (double) stopwatchTicksPerSecond;
                double floatRet = ((double) resultQ) + fraction;
                Assert.True(resultQ <= long.MaxValue && resultQ >= long.MinValue);
                return (floatRet, (long) resultQ);

            }
            Int128 ConvertTsTicksToSwTicks(in Int128 tsTicksToConvert)
            {
                Int128 ret = tsTicksToConvert * stopwatchTicksPerSecond / timespanTicksPerSecond;
                Assert.True(ret == tsTicksToConvert * stopWatchTicksReduced / timespanTicksReduced );
                return ret;
            }

            Int128 ConvertSwTicksToTsTicks(in Int128 swTicks)
            {
                Int128 ret = swTicks * timespanTicksPerSecond / stopwatchTicksPerSecond;
                Assert.True(ret == swTicks * timespanTicksReduced / stopWatchTicksReduced);
                return ret;
            }

            
        }
        private static ulong Gcd(ulong a, ulong b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }
            return a | b;
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
