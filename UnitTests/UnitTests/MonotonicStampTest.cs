using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using HpTimeStamps;
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

        private IEnumerable<TimeSpan> GetNRandomTimespans(int numSpans)
        {
            if (numSpans < 0) throw new ArgumentOutOfRangeException(nameof(numSpans), numSpans, "Value may not be negative.");
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
            if (arr.IsDefault) throw new ArgumentException("The array is not properly initialized.", nameof(arr));
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
                PortableDuration pdFromD = (PortableDuration) d;
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
                : CalculateEpsilon(MonotonicStampFixture.StampContext.TicksPerSecond, PortableDuration.TicksPerSecond);//((double) MonotonicStampFixture.StampContext.TicksPerSecond / PortableDuration.TicksPerSecond * 75.0) ;
            Assert.False(double.IsNaN(portable));
            Assert.False(double.IsNaN(notPortable));
            Assert.False(double.IsPositiveInfinity(portable) || double.IsNegativeInfinity(portable) || double.IsPositiveInfinity(notPortable) || double.IsNegativeInfinity(notPortable));

            double absVOfDiff = Math.Abs(portable - notPortable);
            if (absVOfDiff > epsilon)
            {
                Assert.False(true,
                    $"The absolute distance between {portable:N} and {notPortable:N} is {absVOfDiff:N} which is greater than {epsilon:N}.");
            }

            double CalculateEpsilon(double monotonicTicksPerSecond, double portableTicksPerSecond)
            {
                double greater = Math.Max(monotonicTicksPerSecond, portableTicksPerSecond);
                double lesser = Math.Min(monotonicTicksPerSecond, portableTicksPerSecond);
                return (1.0 - (lesser / greater));
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
