using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using HpTimeStamps;
using HpTimeStamps.BigMath;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using MonotonicStampContext = HpTimeStamps.MonotonicStampContext;
namespace UnitTests
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicStampContext>;
    using MonotonicStampSource = MonotonicTimeStampUtil<MonotonicStampContext>;
    public class MonotonicStampTest : OutputHelperAndFixtureHavingTests<MonotonicStampFixture>
    {
        public MonotonicStampTest([NotNull] ITestOutputHelper helper, [NotNull] MonotonicStampFixture fixture)
            : base(helper, fixture)
        {
            const long portableDurationTicksPerSecond = 1_000_000_000;
            const long tsTicksPerSecond = 10_000_000;
            Helper.WriteLine("Unit test background data.");
            Assert.Equal(tsTicksPerSecond, TimeSpan.TicksPerSecond );
            Assert.Equal(portableDurationTicksPerSecond, PortableDuration.TicksPerSecond);
            Helper.WriteLine("TimeSpan ticks per second: {0:N0}", tsTicksPerSecond );
            Helper.WriteLine("Duration ticks per second: {0:N0}", Duration.TicksPerSecond);
            Helper.WriteLine("Portable duration ticks per second: {0:N0}", PortableDuration.TicksPerSecond);
            Assert.Equal(Duration.EasyConversionsToAndFromTimeSpan, MonotonicStamp.StatContext.EasyConversionToAndFromTimespanTicks);
            Assert.Equal(Duration.EasyConversionsToAndFromPortableDuration, PortableDuration.EasyConversionToAndFromDuration);
            Assert.Equal(Duration.EasyConversionsToAndFromPortableDuration, MonotonicStamp.StatContext.EasyConversionToAndFromNanoseconds);
            Helper.WriteLine("Conversions to and from Timespan: {0}",
                Duration.EasyConversionsToAndFromTimeSpan ? "EASY" : "HARD");
            Helper.WriteLine("Conversions to and from portable duration: {0}",
                PortableDuration.EasyConversionToAndFromDuration ? "EASY" : "HARD");

        }
        internal Random RGen => _rgen.Value;
        
        [Fact]
        public void PrintContextInfo()
        {
            Assert.True(MonotonicStampFixture.StampContextLocalBeginReference.ToUniversalTime() == MonotonicStampFixture.StampContextUtcBeginReference);
            Assert.True(MonotonicStampFixture.TickFrequency == Stopwatch.Frequency);
            Assert.True(MonotonicStampFixture.TimespanFrequency == TimeSpan.TicksPerSecond);
            Helper.WriteLine("Local reference time: [{0:O}].", MonotonicStampFixture.StampContextLocalBeginReference);
            Helper.WriteLine("Utc reference time: [{0:O}].", MonotonicStampFixture.StampContextUtcBeginReference);
            Helper.WriteLine("Local utc offset: [{0:N3}] hours.", MonotonicStampFixture.UtcLocalOffset.TotalHours);
            Helper.WriteLine("Stopwatch tick equivalent to local time: [{0:N}]",
                MonotonicStampFixture.StopwatchTickEquivalentToRefTime);
            Helper.WriteLine("Easy conversion all around: [{0}]", MonotonicStampFixture.StampContext.EasyConversionAllWays);
            Helper.WriteLine("Easy conversions between stopwatch ticks and timespan ticks: [{0}]", MonotonicStampFixture.StampContext.EasyConversionToAndFromTimespanTicks);
            Helper.WriteLine("Easy conversions between stopwatch ticks and nanoseconds: [{0}]", MonotonicStampFixture.StampContext.EasyConversionToAndFromNanoseconds);

            Helper.WriteLine("Duration frequency: {0:N0} ticks per second.", Duration.TicksPerSecond);
            Helper.WriteLine("Timespan frequency: {0:N0} ticks per second.", TimeSpan.TicksPerSecond);
            Helper.WriteLine("{0}: {1:N0}", nameof(MonotonicStamp.TheToTsTickConversionFactorNumerator),
                MonotonicStamp.TheToTsTickConversionFactorNumerator);
            Helper.WriteLine("{0}: {1:N0}", nameof(MonotonicStamp.ToToTsTickConversionFactorDenominator),
                MonotonicStamp.ToToTsTickConversionFactorDenominator);
            MonotonicStamp now = Fixture.StampNow;
            Helper.WriteLine("Initial local stamp: [{0:O}].", now.ToLocalDateTime());
            Helper.WriteLine("Initial utc stamp: [{0:O}].", now.ToUtcDateTime());
        }

        [Fact]
        public void TestTimespanToDurationConversions()
        {
            TimeSpan expectedFiveMilliseconds = TimeSpan.FromMilliseconds(5);
            Duration fiveMilliseconds = Duration.FromMilliseconds(5);
            Duration fiveThousandMicroseconds = Duration.FromMicroseconds(5000);
            TimeSpan asTs = (TimeSpan) fiveMilliseconds;
            TimeSpan asTsExpressedMicro = (TimeSpan) fiveThousandMicroseconds;
            AssertDoubleEqual(asTs.TotalMilliseconds, fiveMilliseconds.TotalMilliseconds);
            Duration roundTripped = (Duration) asTs;
            Assert.True(Duration.AreValuesCloseEnoughAfterConversionToTimeSpan(in roundTripped, in fiveMilliseconds) && (
                        Duration.AreValuesCloseEnough(in roundTripped, asTsExpressedMicro) &&
                        Duration.AreValuesCloseEnough(in fiveThousandMicroseconds, asTsExpressedMicro) &&
                        Duration.AreValuesCloseEnough(fiveThousandMicroseconds, expectedFiveMilliseconds)));
        }

        [Fact]
        public void TestPortableDurationToDurationConversions()
        {
            const int numTests = 1_000_000;
            const int updateEveryXTests = 50_000;
            for (int i = 1; i <= numTests; ++i)
            {
                if (i % updateEveryXTests == 0)
                    Helper.WriteLine("On test {0:N0} of {1:N0}.", i, numTests);
                byte[] bytes = new byte[8];
                RGen.NextBytes(bytes);
                long tsTicks = BitConverter.ToInt64(bytes, 0);
                TestPortableDurationDurationConversions(i, tsTicks);
            }
            Helper.WriteLine("All {0:N0} tests PASSED.", numTests);
        }

        [Fact]
        public void TestPortableDurationSerDeser()
        {
            const int numSerializations = 10_000;
            ImmutableArray<PortableDuration> durations = GetNRandomTimespans(numSerializations)
                .Select(itm => (PortableDuration) itm).ToImmutableArray();
            Assert.Equal(numSerializations, durations.Length);
            ImmutableArray<PortableDuration> results = DoTestPortableDurationSerDeser(durations);
            Assert.Equal(numSerializations, results.Length);
            for (int i = 0; i < numSerializations; ++i)
            {
                ref readonly PortableDuration original = ref durations.ItemRef(i);
                ref readonly PortableDuration roundTripped = ref results.ItemRef(i);
                Assert.True(original == roundTripped,
                    $"Item# {(i + 1):N0} of {numSerializations:N0} had a different original from round tripped value.  Original: [{original}]; Roundtripped: [{roundTripped}].");
            }
        }

        [Fact]
        public void DoTimespanConversionTests()
        {
            const int numTests = 10_000_000;
            const int updateEveryXTests = 500_000;
            Span<byte> bytes = stackalloc byte[8];
            for (int i = 1; i <= numTests; ++i)
            {
                if (i % updateEveryXTests == 0)
                    Helper.WriteLine("On test {0:N0} of {1:N0}.", i, numTests);
                {
                    RGen.NextBytes(bytes);
                    ReadOnlySpan<byte> roBytes = bytes;
                    long tsTicks = BitConverter.ToInt64(roBytes);
                    TestTimeSpanDurationConversions(tsTicks);
                }
            }
            Helper.WriteLine("All {0:N0} tests PASSED.", numTests);
        }

        [Fact]
        public void TestTimespanConversionFailureTestCaseOne()
        {
            const long val = -7_670_048_174_861_859_330;
            TestTimeSpanDurationConversions(val);
        }

        [Fact]
        public void TestTimespanConversionFailureCaseTwo()
        {
            const long val = 5_519_003_985_427_299_254;
            TestTimeSpanDurationConversions(val);
        }


        [Fact]
        public void TestPortableDurationConversionFailureCaseOne()
        {
            const long failingVal = -8_101_250_228_723_645_404;
            TestPortableDurationDurationConversions(1, failingVal);
        }

        [Fact]
        public void TestPortableDurationConversionFailureCaseTwo()
        {
            const long failingVal = -8_356_803_867_519_737_568;
            TestPortableDurationDurationConversions(1, failingVal);
        }

        [Fact]
        public void TestPortableDurationConversionFailureCaseThree()
        {
            const long failingVal = 5_571_665_178_173_090_056;
            TestPortableDurationDurationConversions(1, failingVal);
        }

        [Fact]
        public void PortableDurationConversionFailureTwoElaborationOne()
        {
            const long timespanTicks = -8_356_803_867_519_737_568;
            TimeSpan originalTs = TimeSpan.FromTicks(timespanTicks);
            PortableDuration pd = originalTs;
            Assert.True(pd.Ticks == (originalTs.Ticks * PortableDuration.TicksPerSecondInternal / TimeSpan.TicksPerSecond));
            Assert.True(originalTs.Ticks == (pd.Ticks * TimeSpan.TicksPerSecond / PortableDuration.TicksPerSecondInternal   ));
            TimeSpan roundTripped = (TimeSpan) pd;
            Assert.True(roundTripped.Ticks == timespanTicks);
        }
        [Fact]
        public void TestPortableDurationConversionFailureCaseFour()
        {
            const long failingVal = -6_433_771_731_613_161_268;
            TestPortableDurationDurationConversions(1, failingVal);
        }

        [Fact]
        public void TestDateTimeRoundTrippingNanosecondScaleBreakdown()
        {
            DateTime theDateTime = new DateTime(1919, 2, 3, 17, 15, 42, 981, DateTimeKind.Utc) +
                                   TimeSpan.FromMilliseconds(0.6255);
            Helper.WriteLine("Date time to breakdown: [{0:O}].", theDateTime);
            (int originalWholeDays, int originalWholeSeconds, int originalWholeNanoseconds,
                Int128 originalTotalNanoseconds) = BreakDownDateTimeNanosecondsScale(theDateTime);
            Helper.WriteLine(
                "Original whole days: [{0:N0}]; Original whole seconds [{1:N0}]; Original whole nanoseconds [{2:N0}]; Total nanoseconds original date time: [{3}].",
                originalWholeDays, originalWholeSeconds, originalWholeNanoseconds, originalTotalNanoseconds);
            DateTime andBack = ConstructUtcDateTimeFromNanosecondsScaleBreakdown(originalWholeDays,
                originalWholeSeconds, originalWholeNanoseconds, originalTotalNanoseconds);
            Assert.Equal(theDateTime, andBack);
            Helper.WriteLine("Round tripped date time: [{0:O}].", andBack);
            Helper.WriteLine("Round tripped date time as local date time: [{0:O}].", andBack.ToLocalTime());

            PortableMonotonicStamp stamp = new PortableMonotonicStamp(originalTotalNanoseconds);
            Helper.WriteLine("Portable monotonic stamp based on total nanoseconds (stamp.ToString()): [{0}].", stamp.ToString());
            Helper.WriteLine("Portable monotonic stamp converted utc date time: [{0:O}].", stamp.ToUtcDateTime());
            Helper.WriteLine("Portable monotonic stamp converted to local date time: [{0:O}].", stamp.ToLocalDateTime());

            
        }

        [Fact]
        public void TestMonotonicStampRoundTripDurationScale()
        {
            MonotonicStamp stamp = Fixture.StampNow;
            (int wholeDays, int wholeSeconds, int wholeNanoseconds, decimal fractionalNanoseconds, Int128 totalWholeNanoseconds)=  BreakdownMonotonicStampNanosecondsScale(stamp);
            Helper.WriteLine(
                "For monotonic stamp [{0}], nanoseconds breakdown: [{1:N0}] whole days + [{2:N0}] whole seconds + [{3:N0}] whole nanoseconds + [{4:N}] fractional nanoseconds.",
                stamp, wholeDays, wholeSeconds, wholeNanoseconds, fractionalNanoseconds);
            Helper.WriteLine("Total whole nanoseconds for that stamp: [{0}].", totalWholeNanoseconds);
        }

        private IEnumerable<TimeSpan> GetNRandomTimespans(int numSpans)
        {
            if (numSpans < 0) throw new ArgumentOutOfRangeException(nameof(numSpans), numSpans, @"Value may not be negative.");
            while (numSpans-- > 0)
            {
                yield return RandomSpan;
            }
        }

        private TimeSpan RandomSpan
        {
            get
            {
                Span<byte> bytes =  stackalloc byte[8];
                RGen.NextBytes(bytes);
                ReadOnlySpan<byte> roBytes = bytes;
                long tsTicks = BitConverter.ToInt64(roBytes);
                return TimeSpan.FromTicks(tsTicks);
            }
        }
        

        private ImmutableArray<PortableDuration> DoTestPortableDurationSerDeser(ImmutableArray<PortableDuration> arr)
        {
            if (arr.IsDefault) throw new ArgumentException(@"The array is not properly initialized.", nameof(arr));
            var temp = ImmutableArray.CreateBuilder<PortableDuration>(arr.Length);
            for (int i = 0; i < arr.Length; ++i)
            {
                ref readonly PortableDuration dur = ref arr.ItemRef(i);
                string xml = Fixture.DurationSerializerDeserializer.SerializeToString(in dur);
                temp.Add(Fixture.DurationSerializerDeserializer.DeserializeFromString(xml));
            }
            return temp.MoveToImmutable();
        }

        private void TestPortableDurationDurationConversions(int testNo, long tsTicks)
        {
            try
            {
                TimeSpan rootTimeSpan = TimeSpan.FromTicks(tsTicks);
                PortableDuration portableDuration = rootTimeSpan;
                Duration d = (Duration) portableDuration;
                TimeSpan fromD = (TimeSpan) d;

                double rootMilliseconds = rootTimeSpan.TotalMilliseconds;
                double pdMilliseconds = portableDuration.TotalMilliseconds;
                double dMilliseconds = d.TotalMilliseconds;
                AssertDoubleEqual(rootMilliseconds, dMilliseconds);
                AssertPortableDoubleCloseEnough(pdMilliseconds, dMilliseconds);
                AssertPortableDoubleCloseEnough(pdMilliseconds,rootMilliseconds);
                Assert.True(Duration.AreValuesCloseEnough(in d, in portableDuration) && Duration.AreValuesCloseEnough(in d,fromD  ) && Duration.AreValuesCloseEnough(in d, in portableDuration));
                Assert.True(portableDuration == rootTimeSpan && 
                            portableDuration == rootTimeSpan);

            }
            catch (Exception ex)
            {
                Helper.WriteLine(
                    "Portable duration <-> duration conversion test failed on test number [{0:N0}] for value [{1:N0}] ticks because of exception [{2}].",
                    testNo, tsTicks, ex);
                throw;
            }
        }

        private (int WholeDays, int WholeSeconds, int WholeNanoseconds, Int128 TotalNanoseconds) BreakDownDateTimeNanosecondsScale(DateTime dt)
        {
            Int128 billion = 1_000_000_000;
            Int128 nanosecondPerDay = ((Int128) 86_400) * billion;
            dt = dt.ToUniversalTime();
            long dtTicks = dt.Ticks;
            Int128 ticks = dtTicks;
            ticks *= 100; //convert timespan ticks to nanoseconds
            (Int128 wholeDays, Int128 _) = Int128.DivRem( ticks , nanosecondPerDay);
            Int128 remainingNanoseconds = ticks - (wholeDays * nanosecondPerDay);
            Assert.True(wholeDays <= int.MaxValue);
            Int128 wholeSeconds;
            (wholeSeconds, remainingNanoseconds) = Int128.DivRem(remainingNanoseconds, billion);
            Assert.True(wholeDays <= int.MaxValue);
            Assert.True(remainingNanoseconds <= int.MaxValue);
            
            return ((int) wholeDays, (int) wholeSeconds, (int) remainingNanoseconds, ticks);

        }

        private DateTime ConstructUtcDateTimeFromNanosecondsScaleBreakdown(int wholeDays, int wholeSeconds,
            int wholeNanoseconds, Int128? verifyNanosecondsTotal = null)
        {
            Int128 billion = 1_000_000_000;
            Int128 nanosecondPerDay = ((Int128)86_400) * billion;
            
            Int128 daysToNanoseconds = wholeDays * nanosecondPerDay;
            Int128 wholeSecondsNanoseconds = wholeSeconds * billion;
            Int128 remainingNanoseconds = wholeNanoseconds;

            Int128 totalNanoseconds = daysToNanoseconds + wholeSecondsNanoseconds + remainingNanoseconds;
            Assert.True(verifyNanosecondsTotal == null || verifyNanosecondsTotal == totalNanoseconds);

            (Int128 totalAsDateTimeTicks, Int128 discardedNanoseconds) = Int128.DivRem(in totalNanoseconds, 100);
            DateTime dt = new DateTime((long) totalAsDateTimeTicks, DateTimeKind.Utc);
            return dt;

        }


        

        private (int WholeDays, int WholeSeconds, int WholeNanoseconds, decimal FractionalNanoseconds, Int128
            TotalWholeNanoseconds)  BreakdownMonotonicStampNanosecondsScale(MonotonicStamp stamp)
        {
            var (utcReferenceTime, offsetFromReference, localUtcOffset) = stamp.Value;

            DateTime stampAsUtcDateTime = stamp.ToUtcDateTime();
            Int128 durationTps = Duration.TicksPerSecond;
            Int128 dateTimeTps = TimeSpan.TicksPerSecond;
            Int128 pdTicksPerSecond = PortableDuration.TicksPerSecond;
            
            Int128 timespanTicksForUtcReferenceTime = utcReferenceTime.Ticks;

            Duration tsTicksForUtcRefAsDuration = Duration.FromTimespanTicks((long) timespanTicksForUtcReferenceTime);

            (Int128 durationTicksForUtcReferenceTime, Int128 remainderTicks) =
                Int128.DivRem(timespanTicksForUtcReferenceTime * Duration.TicksPerSecond, dateTimeTps);

            Helper.WriteLine("Remainder ticks: [{0:N}].", ((decimal)(long)remainderTicks) / (long) dateTimeTps);
            Assert.True(durationTicksForUtcReferenceTime <= long.MaxValue &&
                   tsTicksForUtcRefAsDuration.Ticks <= long.MaxValue);
            Helper.WriteLine("tsTicksForUtcRefAsDuration: [{0:N}]; durationTicksForUtcReferenceTime: [{1:N}]",
                (long) tsTicksForUtcRefAsDuration.Ticks, (long) durationTicksForUtcReferenceTime);
            Assert.Equal(tsTicksForUtcRefAsDuration.Ticks, durationTicksForUtcReferenceTime);


            Int128 durationTicksWithOffset = (tsTicksForUtcRefAsDuration + offsetFromReference).Ticks;

            
            
            Int128 durationTicksBackToTimespanTicks =
                durationTicksWithOffset * dateTimeTps / Duration.TicksPerSecond;

            Assert.True(durationTicksBackToTimespanTicks < long.MaxValue);
            DateTime roundTrippedWithOffsetFromReference = new DateTime((long) durationTicksBackToTimespanTicks, DateTimeKind.Utc);
            

            Helper.WriteLine("Expected stamp (monotonic utc): [{0:O}]; Actual round trip: [{1:O}].", stampAsUtcDateTime, roundTrippedWithOffsetFromReference);
            PortableDuration tolerance = PortableDuration.FromMicroseconds(2.5);
            TimeSpan oneMicro = (TimeSpan) tolerance;
            Assert.Equal(stampAsUtcDateTime, roundTrippedWithOffsetFromReference, oneMicro);

            (Int128 totalWholeNanoseconds, Int128 fractionalTicks) =
                Int128.DivRem(durationTicksWithOffset * pdTicksPerSecond, durationTps);
            Assert.True(fractionalTicks <= long.MaxValue);
            long fractionalTicks64 = (long) fractionalTicks;
            decimal durationTpsDec = (decimal) durationTps;
            decimal fractionalNanoseconds = fractionalTicks64 / durationTpsDec;

            Int128 billion = 1_000_000_000;
            Int128 nanosecondPerDay = ((Int128)86_400) * billion;

            int wholeDays = (int) (totalWholeNanoseconds / nanosecondPerDay);
            Int128 remainingNanoseconds = totalWholeNanoseconds - (wholeDays * nanosecondPerDay);
            Int128 wholeSeconds128;
            (wholeSeconds128, remainingNanoseconds) = Int128.DivRem(remainingNanoseconds, billion);
            Assert.True(wholeSeconds128 <= int.MaxValue && remainingNanoseconds <= int.MaxValue);
            int wholeSeconds = (int) wholeSeconds128;
            int wholeNanoSeconds = (int) remainingNanoseconds;
            return (wholeDays, wholeSeconds, wholeNanoSeconds, fractionalNanoseconds, totalWholeNanoseconds);
        }


        private void TestTimeSpanDurationConversions(long tsTicks)
        {
            TimeSpan asTs = TimeSpan.FromTicks(tsTicks);
            try
            {
                Duration d = (Duration) asTs;
                TimeSpan andBack = (TimeSpan) d;
                Assert.True(Duration.AreValuesCloseEnough(in d, asTs));
                Assert.True(Duration.AreValuesCloseEnough(in d, andBack)); 
                AssertTimeSpansCloseEnough(andBack, asTs);
            }
            catch (Exception ex)
            {
                Helper.WriteLine(
                    "TimeSpan Duration conversion tests failed for value [{0:N0}] ticks because of exception [{1:N0}].",
                    tsTicks, ex);
                throw;
            }
        }

        private void AssertPortableDoubleCloseEnough(double portable, double notPortable)
        {
            double epsilon = PortableDuration.TicksPerSecond == MonotonicStampFixture.StampContext.TicksPerSecond
                ? 0.25
                : 1.0 ;
            Assert.False(double.IsNaN(portable));
            Assert.False(double.IsNaN(notPortable));
            Assert.False(double.IsPositiveInfinity(portable) || double.IsNegativeInfinity(portable) || double.IsPositiveInfinity(notPortable) || double.IsNegativeInfinity(notPortable));

            double absVOfDiff = Math.Abs(portable - notPortable);
            if (absVOfDiff > epsilon)
            {
                Assert.False(true,
                    $"The absolute distance between {portable:N} and {notPortable:N} is {absVOfDiff:N} which is greater than {epsilon:N}.");
            }
        }

        private void AssertDoubleEqual(double expected, double actual, double epsilon = 0.5)
        {
            if (double.IsNaN(expected))
            {
                Assert.True(double.IsNaN(actual));
            }
            if (double.IsNaN(actual))
            {
                Assert.True(double.IsNaN(expected));
            }
            if (double.IsInfinity(expected) || double.IsInfinity(actual))
            {
                Assert.True(double.IsPositiveInfinity(expected) == double.IsPositiveInfinity(actual) &&
                            double.IsNegativeInfinity(expected) == double.IsNegativeInfinity(actual));
            }

            double absDiff = Math.Abs(expected - actual);
            if (absDiff > epsilon)
            {
                Assert.False(true,
                    $"The absolute distance between {expected:N} and {actual:N} is {absDiff:N} which is greater than {epsilon:N}.");
            }
        }

        private void AssertTimeSpansCloseEnough(TimeSpan first, TimeSpan second)
        {
            const double diffMustBeLessThan = 1.0;
            double diff = Math.Abs((first.TotalMilliseconds - second.TotalMilliseconds));
            Assert.True(diff < diffMustBeLessThan);
        }

        private static ThreadLocal<Random> _rgen = new ThreadLocal<Random>(() => new Random(), false);
    }
}
