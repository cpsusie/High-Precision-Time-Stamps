using System;
using HpTimeStamps;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class HighPrecisionStampTests : OutputHelperAndFixtureHavingTests<HighPrecisionStampFixture>
    {
        public HighPrecisionStampTests([NotNull] ITestOutputHelper helper, [NotNull] HighPrecisionStampFixture fixture) : base(helper, fixture)
        {
        }

        [Fact]
        public void TestRandomTimeArithmetic()
        {
            int testNo = -1;
            const int numTests = 1000;
            try
            {
                for (testNo = 1; testNo <= numTests; ++testNo)
                {
                    TestStampAgainstTsAndDurationArithmetic(testNo, numTests);
                }
            }
            catch (Exception ex)
            {
                Helper.WriteLine("TEST NUMBER {0} of {1} FAILED: [" + ex.Message + "].", testNo, numTests);
                throw;
            }
        }

        [Fact]
        public void TestUtcStampCorrelationWithMain()
        {
            TimeSpan maxAcceptableDifference = TimeSpan.FromMilliseconds(25);

            if (HpTimeStamps.TimeStampSource.NeedsCalibration)
            {
                HpTimeStamps.TimeStampSource.Calibrate();
            }

            
            DateTime sysClockNow = DateTime.Now;
            DateTime now = Fixture.HpStampSource.Now;
            DateTime nowUtc = now.ToUniversalTime();
            DateTime utcNow = Fixture.HpStampSource.UtcNow;
            Helper.WriteLine("System clock now:\t\t\t\t\t\t[{0:O}].", sysClockNow);
            Helper.WriteLine("Hp local now:\t\t\t\t\t\t\t[{0:O}].", now);
            Helper.WriteLine("Hp local now converted to utc:\t\t[{0:O}].", nowUtc);
            Helper.WriteLine("Hp utc now:\t\t\t\t\t\t\t[{0:O}].", utcNow);
            Helper.WriteLine("Hp utc now converted to local time:\t[{0:O}].", utcNow.ToLocalTime());


            Helper.WriteLine("nowUtc + offset: [{0:O}]", (nowUtc + Fixture.HpStampSource.LocalOffsetFromUtc));
            DateTime nowLocalMinusOffset = now - Fixture.HpStampSource.LocalOffsetFromUtc;

            Assert.True(nowLocalMinusOffset == nowUtc && nowUtc + Fixture.HpStampSource.LocalOffsetFromUtc == now);
            
            TimeSpan difference = (now - sysClockNow).Duration();
            Assert.True(difference <= maxAcceptableDifference,
                "System clock reading: [" + sysClockNow.ToString("O") + "], hp reading: [" + now.ToString("O") +
                $"]: DIFFERENTIAL (value: [{maxAcceptableDifference.TotalMilliseconds:N3} milliseconds]) EXCEEDS max permitted difference of {maxAcceptableDifference.TotalMilliseconds:N3} milliseconds.");
        }

        private void TestStampAgainstTsAndDurationArithmetic(int opNumber, int numTests)
        {
            if (Fixture.HpStampSource.NeedsCalibration)
            {
                Fixture.HpStampSource.CalibrateNow();
            }

            DateTime stamp = HpTimeStamps.TimeStampSource.Now;
            (TimeSpan ts, Duration dur) = Fixture.Between1MillisecondAndOneDay;
            BinaryOpCode operation = Fixture.AddOrSubtract;
            PrintOperation(stamp, ts, in dur, operation);
            (DateTime tsOpResult, DateTime durOpResult) = ExecuteOperation(stamp, ts, in dur, operation);
            PrintResults(tsOpResult, durOpResult);
            ValidateWithinOneMillisecond(tsOpResult, durOpResult);
            (DateTime roundTrippedTsOpResult, DateTime roundTrippedDurOpResult) = ExecuteOperation(stamp, ts, in dur,
                operation == BinaryOpCode.Add ? BinaryOpCode.Subtract : BinaryOpCode.Add);
            Helper.WriteLine("Will now print round tripped results: ");
            PrintResults(roundTrippedTsOpResult, roundTrippedDurOpResult);
            ValidateWithinOneMillisecond(roundTrippedTsOpResult, roundTrippedDurOpResult);
            Helper.WriteLine("Stamp arithmetic test {0} of {1} PASSED.");
            Helper.WriteLine(string.Empty);

            void PrintResults(DateTime dt, DateTime durDateTime)
            {
                Helper.WriteLine("Timespan arithmetic result: {0:O}", dt);
                Helper.WriteLine("Duration arithmetic result: {0:O}", durDateTime);
            }

            (DateTime TimeSpanOperationResult, DateTime DurationOperationResult) ExecuteOperation(DateTime timeStamp,
                TimeSpan span, in Duration d, BinaryOpCode op)
            {
                DateTime spanRes, durRes;
                Assert.True(op == BinaryOpCode.Add || op == BinaryOpCode.Subtract);
                if (op == BinaryOpCode.Add)
                {
                    spanRes = timeStamp + span;
                    durRes =  timeStamp + (TimeSpan) d;
                }
                else
                {
                    spanRes = timeStamp - span;
                    durRes = timeStamp - (TimeSpan) d;
                }

                return (spanRes, durRes);
            }

            void PrintOperation(DateTime dt, TimeSpan t, in Duration d, BinaryOpCode op)
            {
                Helper.WriteLine("Stamp arithmetic test {0} of {1}.", opNumber, numTests);
                Helper.WriteLine("Stamp: {0:O}", dt);
                Helper.WriteLine("Timespan: {0:N3} ms (i.e. {1:N} hours, {2:N} minutes, {3:N} seconds and {4:N} milliseconds).", ts.TotalMilliseconds, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                Helper.WriteLine("Duration: {0:N3} ms (i.e. {1:N} hours, {2:N} minutes, {3:N} seconds and {4:N} milliseconds).", d.TotalMilliseconds, d.Hours, d.Minutes, d.Seconds, d.Milliseconds);
                Helper.WriteLine("Operation: {0}.", op);
            }

            void ValidateWithinOneMillisecond(DateTime spanRes, DateTime durRes)
            {
                const double maxDifference = 1.0;
                DateTime bigger = spanRes > durRes ? spanRes : durRes;
                DateTime smaller = spanRes < durRes ? spanRes : durRes;
                TimeSpan difference = bigger - smaller;
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Assert.True(difference.TotalMilliseconds <= maxDifference,
                    $"The arithmetic results {spanRes:O} and {durRes:O} have greater than " +
                    // ReSharper disable once UnreachableCode
                    $"the maximum permitted difference of {maxDifference} {(maxDifference == 1 ? "millisecond" : "milliseconds")}.");
            }
        }
    }
}
