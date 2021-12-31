using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using HpTimeStamps;
using Xunit;
using Xunit.Abstractions;
using Duration = Google.Protobuf.WellKnownTypes.Duration;
using PortableStamp = HpTimeStamps.PortableMonotonicStamp;
using PortableDuration = HpTimeStamps.PortableDuration;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;

namespace UnitTests
{
    using Monostamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    public class TimestampConversionTests
    {
        public ITestOutputHelper Helper { get; }

        public Random Rng
        {
            get
            {
                return s_theRng ??= new Random();
            }
        }

        public TimestampConversionTests(ITestOutputHelper helper) =>
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));

        //[Fact]
        //public async Task TestConversions()
        //{
        //    const int numTests = 1_000;
        //    Task[] tasks = new Task[numTests];
        //    await Task.Run(() =>
        //        Parallel.For(0, numTests,
        //            i => tasks[i] = DoTestConversions(new NumberingPacket(i + 1, numTests))));
        //    await Task.WhenAll(tasks);
        //}

        [Fact]
        public void TestJpsEpoch()
        {
            const long nanoAdj = 11;
            PortableStamp jpsEpoch = new DateTime(1948, 11, 11, 11, 11, 11, DateTimeKind.Utc);
            PortableDuration adjVal = PortableDuration.FromNanoseconds(nanoAdj);
            jpsEpoch += adjVal;
            const string testName = "JPS Epoch Test";
            ExecuteTest(testName, in jpsEpoch, in adjVal);
        }

        [Fact]
        public void TestCpsEpoch()
        {
            const long nanoAdj = -123_456_789L;
            PortableStamp cpsEpoch = new DateTime(1978, 6, 10, 16, 20, 20, DateTimeKind.Utc);
            PortableDuration adjVal = PortableDuration.FromNanoseconds(nanoAdj);
            cpsEpoch += adjVal;
            const string testName = "CPS Epoch Test";
            ExecuteTest(testName, in cpsEpoch, in adjVal);
        }

        //private async Task DoTestConversions(NumberingPacket packet)
        //{
        //    (int testNum, int numTests) = packet;
        //    try
        //    {
        //        Helper.WriteLine($"Starting test# {testNum} of {numTests}....");
        //        //const int portableStampIndex = 0;
        //        //const int monostampIndex = 1;
        //        //const int utcDateTimeIndex = 2;
        //        //const int localDateTimeIndex = 3;
        //        //const int randomKindIndex = 4;

        //        Monostamp monoStamp = MonostampSrc.StampNow;
        //        PortableStamp portableStamp = (PortableStamp)monoStamp;
        //        DateTime utcDateTime = (DateTime)portableStamp;
        //        DateTime localDateTime = (DateTime)portableStamp;
        //        DateTimeKind dtk = Rng.Next(0, 3) switch
        //        {
        //            0 => DateTimeKind.Utc,
        //            1 => DateTimeKind.Local,
        //            _ => DateTimeKind.Unspecified
        //        };

        //        DateTime dateTimeWithRandomKind = new DateTime(1948, 11, 11, 11, 11, 11, dtk);
        //        TimeSpan fractionalSeconds = TimeSpan.FromTicks(1_111_111);
        //        Assert.True(fractionalSeconds < TimeSpan.FromSeconds(1));
        //        dateTimeWithRandomKind += fractionalSeconds;

        //        Task[] tasks =
        //        {
        //            Task.Run(() => TestStampConversion(portableStamp, packet)),
        //            Task.Run(() => TestStampConversion(monoStamp, in packet)),
        //            Task.Run(() => TestStampConversion(utcDateTime, packet)),
        //            Task.Run(() => TestStampConversion(localDateTime, packet)),
        //            Task.Run(() => TestStampConversion(dateTimeWithRandomKind, packet))
        //        };

        //        await Task.WhenAll(tasks);
        //        Helper.WriteLine($"test# {testNum} of {numTests} PASSED{Environment.NewLine}");

        //    }
        //    catch (Exception ex)
        //    {
        //        Helper.WriteLine(
        //            $"test# {testNum} of {numTests}: FAILED with exception: {Environment.NewLine}\"{ex}\"");
        //        throw;
        //    }
        //}

        //private Task TestStampConversion(Monostamp testMe, in NumberingPacket packet)
        //{
        //    Monostamp stamp = testMe;
        //    Timestamp ts = stamp.ToProtobufStamp();
        //    Monostamp roundTripped = (Monostamp)ts.ToPortableStamp();
        //    Duration difference = (roundTripped - stamp).AbsoluteValue();
        //    Helper.WriteLine(
        //        $"{PopulateTestString(in packet)} -- Monostamp Conversion Difference: [{difference.TotalMicroseconds:N1} microseconds].");
        //    Assert.True(difference <= MaxAcceptableDifferenceMono);
        //    return Task.CompletedTask;
        //}

        private static string PopulateTestString(in NumberingPacket packet) =>
            $"test# {packet.TestNumber} of {packet.NumberOfTests} difference:";

        private void ExecuteTest(string testName, in PortableStamp testStamp, in PortableDuration nanoAdjust)
        {
            Helper.WriteLine($"Executing \"{testName}\" test for [" + testStamp + "]:");

            (long wholeSeconds, long remainder) =
                (testStamp - PortableStamp.UnixEpochStamp).GetTotalWholeSecondsAndRemainder();
            (long secs, long nanos) = (wholeSeconds, remainder) switch
            {
                (var w, var f) when w != 0 && f < 0 => throw new InvalidProtobufStampException(wholeSeconds, (int)remainder, "Bad value!"),
                (0, var frac) => (0, frac),
                (var whole, 0L) => (wholeSeconds, 0L),
                ( > 0, var frac) => (wholeSeconds + 1, frac),
                ( < 0, var frac) => (wholeSeconds - 1, frac),
            };
            Timestamp stamp = new Timestamp
            {
                Seconds = secs,
                Nanos = (int)nanos,
            };
            Helper.WriteLine($"Converted to protostamp: {stamp}.  Seconds: {stamp.Seconds}; Nanos: {stamp.Nanos}");
            Timestamp dtVersion = Timestamp.FromDateTime((DateTime)testStamp);
            Duration nanosExtra = new Duration { Nanos = (int) nanoAdjust.TotalNanoseconds};
            dtVersion += nanosExtra;
            Helper.WriteLine($"Constructed from DateTime version: {dtVersion}.  Seconds: {dtVersion.Seconds}; Nanos: {dtVersion.Nanos}.");

            PortableDuration wholeSecsDur = PortableDuration.FromSeconds(stamp.Seconds);
            PortableStamp roundTrippedStamp = PortableStamp.UnixEpochStamp + wholeSecsDur + nanoAdjust;
          
            Helper.WriteLine($"RoundTrippedStamp: [{roundTrippedStamp}].");
            Assert.True(dtVersion == stamp);
            Assert.True(roundTrippedStamp == testStamp && roundTrippedStamp.GetHashCode() == testStamp.GetHashCode());
        }


        //private async Task TestStampConversion(DateTime testMe, NumberingPacket packet)
        //{
        //    Assert.True(EnumCastingUtil<DateTimeKind, int>.CastToEnum(default(int)) == DateTimeKind.Unspecified);
        //    if (testMe.Kind.ValueOrDefaultIfNDef() == DateTimeKind.Unspecified)
        //    {
        //        await TestUnspecified(testMe, Helper, packet);
        //    }
        //    else
        //    {
        //        (DateTime utcVersion, DateTime localVersion) = testMe.Kind switch
        //        {
        //            DateTimeKind.Utc => (testMe, testMe.ToLocalTime()),
        //            DateTimeKind.Local => (testMe.ToUniversalTime(), testMe),
        //            _ => (testMe.ToUniversalTime(), testMe.ToLocalTime())
        //        };

        //        Task[] tasks =
        //        {
        //            Task.Run(() => TestUtc(utcVersion, Helper,  packet)),
        //            Task.Run(() => TestLocal(localVersion, Helper, packet))
        //        };
        //        await Task.WhenAll(tasks);
        //    }

        //    static Task TestLocal(DateTime tm, ITestOutputHelper helper, NumberingPacket packet)
        //    {
        //        Timestamp ts = tm.ToProtobufStamp();
        //        DateTime roundTrip = ts.ToUtcDateTime().ToLocalTime();
        //        TimeSpan difference = (roundTrip - tm).Duration();
        //        Assert.True(difference <= MaxAcceptableDifferenceDateTime);
        //        helper.WriteLine($"{PopulateTestString(in packet)} -- Local Datetime conversion difference: [{difference.TotalMilliseconds:N7} milliseconds].");
        //        return Task.CompletedTask;
        //    }

        //    static Task TestUtc(DateTime tm, ITestOutputHelper helper, NumberingPacket packet)
        //    {
        //        Timestamp ts = tm.ToProtobufStamp();
        //        DateTime roundTrip = ts.ToUtcDateTime();
        //        TimeSpan difference = (roundTrip - tm).Duration();
        //        Assert.True(difference <= MaxAcceptableDifferenceDateTime);
        //        helper.WriteLine($"{PopulateTestString(in packet)} -- UTC Datetime conversion difference: [{difference.TotalMilliseconds:N7} milliseconds].");
        //        return Task.CompletedTask;
        //    }

        //    static Task TestUnspecified(DateTime tm, ITestOutputHelper helper, NumberingPacket packet) => Task.Run(() =>
        //    {
        //        Assert.True(tm.Kind == DateTimeKind.Unspecified);
        //        Timestamp ts = tm.ToProtobufStamp();
        //        DateTime roundTripped = ts.ToUtcDateTime();
        //        TimeSpan difference = (roundTripped - tm.ToUniversalTime()).Duration();
        //        helper.WriteLine($"{PopulateTestString(in packet)} -- UNSPECIFIED Datetime conversion difference: [{difference.TotalMilliseconds:N7} milliseconds].");
        //        Assert.True(difference <= MaxAcceptableDifferenceDateTime);
        //    });

        //}




        //private Task TestStampConversion(in PortableStamp stamp, NumberingPacket packet)
        //{
        //    DateTime asDateTime = (DateTime)stamp;
        //    Timestamp fromDateTime = Timestamp.FromDateTime(asDateTime);
        //    Timestamp fromStamp = stamp.ToProtobufStamp();
        //    Assert.True(fromStamp == fromDateTime);
        //    PortableStamp roundTripped = fromStamp.ToPortableStamp();
        //    PortableDuration difference = (roundTripped - roundTripped).AbsoluteValue();
        //    Assert.True(difference <= MaxAcceptableDifferencePortable);
        //    Helper.WriteLine($"{PopulateTestString(in packet)} -- Portable stamp conversion difference: [{difference.TotalNanoseconds} nanoseconds].",
        //        difference.TotalNanoseconds);
        //    return Task.CompletedTask;
        //}

        private readonly record struct NumberingPacket(int TestNumber, int NumberOfTests);
        [ThreadStatic] private static Random? s_theRng;
        private static readonly PortableDuration MaxAcceptableDifferencePortable = PortableDuration.FromNanoseconds(101);
       
    }
}
