using System;
using System.Collections.Generic;
using System.Text;
using HpTimeStamps;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using MonotonicStampContext = HpTimeStamps.MonotonicStampContext;

namespace UnitTests
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicStampContext>;
    using MonotonicStampSource = MonotonicTimeStampUtil<MonotonicStampContext>;

    public  class StampTests : OutputHelperAndFixtureHavingTests<StampTestFixture>
    {
        public StampTests([NotNull] ITestOutputHelper helper, [NotNull] StampTestFixture fixture) : base(helper,
            fixture)
        {
        }

        [Fact]
        public void TestTimestampComponents()
        {
            MonotonicStamp stamp = Fixture.MonotonicStampNow;
            DateTime localTime = stamp.ToLocalDateTime();
            DateTime utcTime = stamp.ToUtcDateTime();
            TimeSpan maxDiff = TimeSpan.FromSeconds(0.5);
            Assert.True(localTime.ToUniversalTime() == utcTime);
            DateTime localFromSysClock = DateTime.Now;
            DateTime utcFromSysClock = localFromSysClock.ToUniversalTime();
            Assert.True(localFromSysClock.Kind == DateTimeKind.Local && DateTimeKind.Local == localTime.Kind);
            CompareDateTimeComponents(localFromSysClock, localTime, maxDiff);
            CompareDateTimeComponents(utcFromSysClock, utcTime, maxDiff);
        }

        [Fact]
        public void TestPrintMonotonicNow()
        {
            MonotonicStamp stamp = Fixture.MonotonicStampNow;
            string stampText = stamp.ToString();
            DateTime asLocalDateTime = stamp.ToLocalDateTime();
            string dtText = asLocalDateTime.ToString("O");
            Assert.Equal(dtText, stampText);
        }

        [Fact]
        public void DoRandomizedStampTests()
        {
            const int numTests = 500_000;
            for (int testNo = 1; testNo <= numTests; ++testNo)
            {
                if (testNo % 25_000 == 0)
                {
                    Helper.WriteLine("On test {0:N0} of {1:N0}.", testNo, numTests);
                }
                TestRandomizedStamp(testNo);
            }
        }
        
        private void TestRandomizedStamp(int testNo)
        {
            var (stampNow, localEquivNow, stampWithOffset, dtWithOffset, randomOffsetDuration, offsetTs) = Fixture.RandomTimeWithin14DaysOfNow;
            DateTime localEquivOffsetByConvertedDuration = default;
            MonotonicStamp monoStampOffsetByConvertedTs = default;
            try
            {
                Assert.True(stampNow + randomOffsetDuration == stampWithOffset);
                Assert.True(localEquivNow + offsetTs == dtWithOffset);

                localEquivOffsetByConvertedDuration = localEquivNow + (TimeSpan) randomOffsetDuration;
                monoStampOffsetByConvertedTs = stampNow + (Duration) offsetTs;

                CompareMonotonicStamps(stampWithOffset, monoStampOffsetByConvertedTs, Duration.FromMicroseconds(999));
            }
            catch (Exception e)
            {
                Helper.WriteLine("Test# {0:N0} failed because of exception: [{1}].", testNo, e);
                Helper.WriteLine("Monotonic now: [{0}].", stampNow.ToString());
                Helper.WriteLine("Dt equiv: [{0:O}].", localEquivNow);
                Helper.WriteLine("Monotonic with offset: [{0}].", stampWithOffset);
                Helper.WriteLine("Dt equiv: [{0:O}].", dtWithOffset);
                Helper.WriteLine("Offset Duration:  (microseconds: [{0:N1}]; days: [{1:N8}]).", randomOffsetDuration.TotalMicroseconds, randomOffsetDuration.TotalDays);
                Helper.WriteLine("Timespan equivalent of Duration:  (milliseconds: [{0:N4}]; days: [{1:N8}]).", offsetTs.TotalMilliseconds, offsetTs.TotalDays);
                Helper.WriteLine("Dt equivalent offset by duration cast to timespan: [{0:O}].", localEquivOffsetByConvertedDuration);
                Helper.WriteLine("Mono ts offset by timespan cast to duration: [{0}].", monoStampOffsetByConvertedTs.ToString());
                throw;
            }



        }
        
        [Fact]
        public void TestPortables()
        {
            MonotonicStamp monotonicNow = Fixture.MonotonicStampNow;
            PortableMonotonicStamp now = Fixture.PortableStampNow;
            Helper.WriteLine("Portable stamp: {0}.", now);
            MonotonicStamp convertedToMonotonic = (MonotonicStamp) now;
            DateTime local = now.ToLocalDateTime();
            DateTime utc = now.ToUtcDateTime();
            Helper.WriteLine("Monotonic now: [{0}].", monotonicNow);
            Helper.WriteLine("Portable->monotonic: [{0}].", convertedToMonotonic);
            Helper.WriteLine("As local dt: [{0:O}].", local);
            Helper.WriteLine("As utc dt: [{0:O}.]", utc);

            var diff = convertedToMonotonic - monotonicNow;
            if (diff < Duration.Zero) diff = -diff;
            Helper.WriteLine("Difference in monotonic and portable -> monotonic: {0:N6} microseconds.", diff.TotalMicroseconds);
            Assert.True(diff < Duration.FromMilliseconds(1));

        }

        private void CompareMonotonicStamps(MonotonicStamp expected, MonotonicStamp actual, Duration maxDiff)
        {
            Assert.True(maxDiff >= Duration.Zero);
            ref readonly MonotonicStamp greater = ref expected;
            ref readonly MonotonicStamp lesser = ref actual;
            if (expected < actual)
            {
                greater = ref actual;
                lesser = ref expected;
            }
            
            Duration difference = greater - lesser;
            Assert.True(difference <= maxDiff);
        }
        
        private void CompareDateTimeComponents(DateTime sysClock, DateTime stamp, TimeSpan maxFracSecDiff)
        {
            Assert.True(maxFracSecDiff >= TimeSpan.Zero && maxFracSecDiff < TimeSpan.FromSeconds(1));
            Assert.Equal(sysClock.Year, stamp.Year);
            Assert.Equal(sysClock.Month, stamp.Month);
            Assert.Equal(sysClock.Day, stamp.Day);
            Assert.Equal(sysClock, stamp, maxFracSecDiff);
        }
    }
}
