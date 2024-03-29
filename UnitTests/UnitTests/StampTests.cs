﻿using System;
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
        public void TestDateTimePortables()
        {
            DateTime stamp = new DateTime(1919, 06, 10, 20, 32, 41, 321, DateTimeKind.Utc);
            Helper.WriteLine($"An arbitrary date time: [{stamp:O}].");
            PortableMonotonicStamp portableStamp = stamp;
            Helper.WriteLine("As a portable stamp: [{0}].", portableStamp);
            DateTime reversed = (DateTime) portableStamp;
            Helper.WriteLine("Reversed: [{0:O}]", reversed);
            Assert.True(reversed == stamp);
        }

        [Fact]
        public void TestPortableStampBreakdown()
        {
            MonotonicStamp stamp = Fixture.MonotonicStampNow;
            PortableMonotonicStamp portable = (PortableMonotonicStamp) stamp;
            DateTime utcFromStamp = stamp.ToUtcDateTime();
            DateTime utcFromPortable = portable.ToUtcDateTime();
            int millisecondsMonoStampDateTime = utcFromStamp.Millisecond;
            int millisecondsPortableStampDateTime = utcFromPortable.Millisecond;
            Assert.Equal(millisecondsMonoStampDateTime, millisecondsPortableStampDateTime);
            int portableDirectFractionalSeconds = portable.FractionalSeconds;
            //convert to milliseconds before comparison
            Assert.Equal(portableDirectFractionalSeconds / 1_000_000, millisecondsPortableStampDateTime);
            string stampStr = utcFromStamp.ToString("O");
            //string fudge = "1919-06-10T20:32:41.3210000Z";
            //int length = fudge.Length;
            ReadOnlySpan<char> chars = stampStr.AsSpan().Slice(stampStr.Length-8, 7);
            int value = Int32.Parse(chars);
            Assert.Equal(value, portableDirectFractionalSeconds / 100);

            Helper.WriteLine("Here is the original monotonic stamp: [{0}].", stamp);
            Helper.WriteLine("Here is the portable version: [{0}].", portable);
            Helper.WriteLine("Here is the portable version in local time: [{0}].", portable.ToLocalString());
        }

        [Fact]
        public void TestDtStringsIso8601AlwaysSameLength()
        {
            DateTime noFractionalSecondsJan = new DateTime(1919, 1, 1, 1, 1, 1, DateTimeKind.Utc);
            int noFrSecJanLength = GetLength(noFractionalSecondsJan);
            DateTime withFractionalSecondsJan = noFractionalSecondsJan + TimeSpan.FromMilliseconds(123.4567);
            int wFrcSJanLength = GetLength(withFractionalSecondsJan);
            DateTime noFractionalSecondsDec = new DateTime(1919, 12, 12, 17, 17, 59, DateTimeKind.Utc);
            int noFrSd = GetLength(noFractionalSecondsDec);
            DateTime withFracSecDec = noFractionalSecondsDec + TimeSpan.FromMilliseconds(999.9999);
            int wFrSd = GetLength(withFracSecDec);
            
            Assert.True(noFrSecJanLength == wFrcSJanLength &&
                        wFrcSJanLength == noFrSd &&
                        noFrSd == wFrSd);

            Helper.WriteLine("All 8601 datetime stamps are {0:N0} characters long.", noFrSecJanLength);

            static int GetLength(DateTime dt) => dt.ToString("O").Length;
        }

        [Fact]
        public void TestArithmetic()
        {
            DateTime myDt = new DateTime(2069, 06, 10, 18, 42, 49, 333, DateTimeKind.Utc);
            PortableMonotonicStamp stamp = myDt;
            Helper.WriteLine("Date time: \t[{0:O}].", myDt);
            Helper.WriteLine("stamp: \t\t[{0}].", stamp);
            PortableMonotonicStamp plusNano = stamp + PortableDuration.FromNanoseconds(1);
            Helper.WriteLine("Plus nano: \t[{0}].", plusNano);
            Assert.True(plusNano - stamp == PortableDuration.FromNanoseconds(1));
            Assert.True(plusNano > stamp && stamp < plusNano && PortableMonotonicStamp.Compare(in plusNano, in stamp)>0);
            PortableDuration oneMicrosecond = PortableDuration.FromMicroseconds(1);
            TimeSpan oneMicroSecond = (TimeSpan) oneMicrosecond;
            Assert.True(oneMicrosecond == oneMicroSecond);
            PortableMonotonicStamp upOneMicro = plusNano + PortableDuration.FromNanoseconds(999);
            Assert.True(stamp + oneMicrosecond == upOneMicro);
            myDt += oneMicroSecond;
            Assert.True(upOneMicro == myDt);
            string utcDtUpMicro = myDt.ToString("O");
            string localDtUpMicro = myDt.ToLocalTime().ToString("O");
            string utcStampUpMicro = upOneMicro.ToString();
            string localStampUpMicro = upOneMicro.ToLocalString();
            Assert.Equal(localDtUpMicro, localStampUpMicro);
            Assert.Equal(utcDtUpMicro, utcStampUpMicro);
            Helper.WriteLine($"{nameof(utcDtUpMicro)}:\t\t\t[{utcDtUpMicro}].");
            Helper.WriteLine($"{nameof(localDtUpMicro)}:\t\t\t[{localDtUpMicro}].");
            Helper.WriteLine($"{nameof(utcStampUpMicro)}:\t\t[{utcStampUpMicro}].");
            Helper.WriteLine($"{nameof(localStampUpMicro)}:\t\t[{localStampUpMicro}].");

        }
        /// <summary>
        /// Bug 22 fix unit test
        /// </summary>
        [Fact]
        public void TestPortableDurationArithmetic()
        {
            DateTime date = new DateTime(2069, 06, 10, 18, 42, 49, 333, DateTimeKind.Utc);
            TimeSpan span = TimeSpan.FromDays(12.912);
            TestOperation(date, span, (d, s) => d - s, (p, d) => p-d);
        }

        private void TestOperation(DateTime dateTime, TimeSpan ts, Func<DateTime, TimeSpan, DateTime> dtOp,
            Func<PortableMonotonicStamp, PortableDuration, PortableMonotonicStamp> stampOp)
        {
            Assert.NotNull(dtOp);
            Assert.NotNull(stampOp);
            
            PortableMonotonicStamp stamp = dateTime;
            PortableDuration pd = ts;

            Assert.True(dtOp(dateTime, ts) == stampOp(stamp, pd));
        }

        [Fact]
        public void TestPortableStamp()
        {
            MonotonicStamp stamp = Fixture.MonotonicStampNow;
            DateTime utcDtFromStamp = stamp.ToUtcDateTime();
            PortableMonotonicStamp portableStamp = (PortableMonotonicStamp) stamp;
            PortableMonotonicStamp fromDateTime = utcDtFromStamp;
            DateTime fromPortableStampMadeFromMonotonicStamp = (DateTime) portableStamp;
            DateTime fromDateTimeMadeFromUtcDateTime = (DateTime) fromDateTime;
            Assert.True(fromDateTimeMadeFromUtcDateTime == utcDtFromStamp);

            if (fromPortableStampMadeFromMonotonicStamp != fromDateTimeMadeFromUtcDateTime)
            {
                DateTime bigger = fromDateTimeMadeFromUtcDateTime > fromPortableStampMadeFromMonotonicStamp
                    ? fromDateTimeMadeFromUtcDateTime
                    : fromPortableStampMadeFromMonotonicStamp;
                DateTime smaller = fromDateTimeMadeFromUtcDateTime < fromPortableStampMadeFromMonotonicStamp
                    ? fromDateTimeMadeFromUtcDateTime
                    : fromPortableStampMadeFromMonotonicStamp;
                TimeSpan difference = bigger - smaller;
                Assert.True(difference <= TimeSpan.FromMilliseconds(0.001));
                Helper.WriteLine("difference between the datetimes is: [{0:N5}] milliseconds.", difference.TotalMilliseconds);
            }
            else
            {
                Helper.WriteLine("The two date times are identical.");
            }

            if (portableStamp != fromDateTime)
            {
                PortableDuration diff;
                if (portableStamp > fromDateTime)
                {
                    diff = new PortableDuration( portableStamp._dateTimeNanosecondOffsetFromMinValueUtc - fromDateTime._dateTimeNanosecondOffsetFromMinValueUtc);
                }
                else
                {
                    diff = new PortableDuration(fromDateTime._dateTimeNanosecondOffsetFromMinValueUtc - portableStamp._dateTimeNanosecondOffsetFromMinValueUtc);
                }

                Helper.WriteLine(
                    "Difference between portable stamps as imported from monotonic vs imported from TS: [{0:N}].",
                    diff.InternalTicks);
            }
            else
            {
                Helper.WriteLine("The two portable monotonic stamps are identical.");
            }
            

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
