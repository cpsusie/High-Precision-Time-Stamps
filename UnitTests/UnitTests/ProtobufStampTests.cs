﻿using Google.Protobuf.WellKnownTypes;
using System;
using System.Runtime.CompilerServices;
using HpTimeStamps;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Duration = HpTimeStamps.Duration;
using ProtobufStamp = Google.Protobuf.WellKnownTypes.Timestamp;
using PortableStamp = HpTimeStamps.PortableMonotonicStamp;
using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<HpTimeStamps.MonotonicStampContext>;
using MonostampSrc = HpTimeStamps.MonotonicTimeStampUtil<HpTimeStamps.MonotonicStampContext>;

namespace UnitTests
{
    public class ProtobufStampTests
    {
        public ITestOutputHelper Helper { get; }

        public ProtobufStampTests(ITestOutputHelper helper) =>
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
        [Fact]
        public void TestCpsEpoch()
        {
            PortableStamp testMe = new DateTime(1978, 6, 10, 16, 13, 42, DateTimeKind.Utc);
            testMe += PortableDuration.FromNanoseconds(341_521_955);
            TestPortableStampConversion(in testMe, "CPS_EPOCH");
        }

        [Fact]
        public void TestAnotherCpsEpoch()
        {
            const long nanoAdj = -123_456_789L;
            PortableStamp cpsEpoch = new DateTime(1978, 6, 10, 16, 20, 20, DateTimeKind.Utc);
            PortableDuration adjVal = PortableDuration.FromNanoseconds(nanoAdj);
            cpsEpoch += adjVal;
            
            TestPortableStampConversion(in cpsEpoch, "ANOTHER_CPS_EPOCH");
            
        }

            [Fact]
        public void TestJpsEpoch()
        {
            PortableStamp testMe = new DateTime(1948, 11, 11, 11, 11, 11, DateTimeKind.Utc);
            testMe += PortableDuration.FromNanoseconds(11);
            TestPortableStampConversion(in testMe, "JPS_EPOCH");
        }

        [Fact]
        public void TestOneNanoSecondDeviation()
        {
            const long deviationToTest = 1;
            TestNanosecondsDeviationsAroundUnixEpoch(deviationToTest);
        }

        [Fact]
        public void TestOneBillionAndOneNanoSecondDeviation()
        {
            const long deviationToTest = 1_000_000_001L;
            TestNanosecondsDeviationsAroundUnixEpoch(deviationToTest);
        }

        [Fact]
        public void TestMaxNanoSecondDeviation()
        {
            const long deviationToTest = 999_999_999L;
            TestNanosecondsDeviationsAroundUnixEpoch(deviationToTest);
        }

        [Fact]
        public void TestFailureCaseOne()
        {
            
            //_startedAt
            //{ 2333 - 03 - 05T19: 20:17.123456277Z}
            //Day: 5
            //FractionalSeconds: 123456277
            //Hour: 19
            //Minutes: 20
            //Month: 3
            //NanosecondsSinceUtcEpoch: "73,596,262,817,123,456,277 nanoseconds since epoch."
            //Seconds: 17
            //TimeSinceEpoch: { 73,596,262,817,123,456,277 nanoseconds}
            //Year: 2333
            //_dateTimeNanosecondOffsetFromMinValueUtc: { 73596262817123456277}
            //_serialized: null
            //_result.Value
            //{[TestResult] --Round Tripped Stamp: [2333 - 03 - 05T19: 20:17.123455765Z]; Pbf Seconds: [11460684017]; Pbf Nanos: [123456277].}
            //PbfStamp: { HpTimeStamps.ProtobufFormatStamp}
            //RoundTrippedValue: { 2333 - 03 - 05T19: 20:17.123455765Z}
            //_pbfStamp: { HpTimeStamps.ProtobufFormatStamp}
            //_roundTrippedValue: { 2333 - 03 - 05T19: 20:17.123455765Z}
            const long nanoAdj = 123_456_277; 
            const long tickAdj = 123_456_2;
            const byte nanoDifferential = 77;
            Assert.True(TimeSpan.TicksPerSecond == 10_000_000);
            DateTime basisDateTime = new DateTime(2333, 3, 5, 19, 20, 17, DateTimeKind.Utc);
            TimeSpan basisTickAdjustment = TimeSpan.FromTicks(tickAdj);
            var conversionResults = BreakItDown(basisDateTime, basisTickAdjustment, nanoDifferential);
            Assert.NotNull(conversionResults.ConvertedNormalWayPbs);

            PortableStamp failingValue = basisDateTime;
            failingValue += PortableDuration.FromNanoseconds(nanoAdj);
            StampConversionData.CreateConversionData(in failingValue, out StampConversionData convertData);
            Assert.False(convertData.HasResult);

            ProtobufFormatStamp pbfFormat = (ProtobufFormatStamp)failingValue;
            ProtobufStamp protoStampFromFailingValue = new Timestamp
                { Seconds = pbfFormat.Seconds, Nanos = pbfFormat.Nanoseconds };
            Assert.True(pbfFormat.Seconds == protoStampFromFailingValue.Seconds &&
                        pbfFormat.Nanoseconds == protoStampFromFailingValue.Nanos);
            PortableStamp roundTripped = (PortableStamp)pbfFormat;
            TestResult tr = new TestResult(in pbfFormat, in roundTripped);
            convertData.ProvideResultValue(in tr);

            Assert.True(convertData.HasResult);
            if (!convertData.ResultPasses)
            {
                Helper.WriteLine(
                    $"Test failed!  Started with: [{failingValue}], Got ProtoStamp with: " +
                    $"[(seconds: {convertData.Results.PbfStamp.Seconds:N0}, " +
                    $"nanos: {convertData.Results.PbfStamp.Nanoseconds:N0})], " +
                    $"Round tripped result: [{convertData.Results.RoundTrippedValue.ToString()}]." +
                    " Difference between round trip and starting: [" +
                    (convertData.Results.RoundTrippedValue - convertData.TestStamp).TryGetTotalNanoseconds()
                    ?.ToString("N0") + "].");
            }
            Assert.True(convertData.ResultPasses);
        }

        private (PortableStamp ConvertedNormalWay, ProtobufFormatStamp ConvertedNormalWayPbf, ProtobufStamp
            ConvertedNormalWayPbs, ProtobufFormatStamp NormalWayRoundTripToPbf, PortableStamp RoundTrippedNormal, PortableStamp ConvertedAlternateWay, ProtobufFormatStamp ConvertedAlternateWayPbf,
            ProtobufStamp ConvertedAlternateWayPbs)
            BreakItDown(DateTime startAt, TimeSpan addToMe, byte extraNanos)
        {
            ProtobufStamp alternate = ProtobufStamp.FromDateTime(startAt.ToUniversalTime());
            Google.Protobuf.WellKnownTypes.Duration extra =
                Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(addToMe) +
                new Google.Protobuf.WellKnownTypes.Duration() { Seconds = 0, Nanos = extraNanos };
            alternate += extra;
            Helper.WriteLine("Using alternate protobuf stamp: [{0}].", alternate);
            ProtobufFormatStamp altPbfFromPbs = new ProtobufFormatStamp(alternate.Seconds, alternate.Nanos);
            PortableStamp alternateFromPbs = (PortableStamp)altPbfFromPbs;
            Helper.WriteLine("Alternate pbf from pbs: [{0}].", altPbfFromPbs);
            Helper.WriteLine("Alternate stamp from pbs: [{0}].", alternateFromPbs);

            PortableStamp normalWay = startAt;
            PortableDuration offset = (addToMe + PortableDuration.FromNanoseconds(extraNanos));
            normalWay += offset;
            ProtobufFormatStamp normalPbf = (ProtobufFormatStamp) normalWay;
            ProtobufStamp normalPbs = new ProtobufStamp { Seconds = normalPbf.Seconds, Nanos = normalPbf.Nanoseconds };
            ProtobufFormatStamp rtNw = new ProtobufFormatStamp(normalPbs.Seconds, normalPbs.Nanos);
            PortableStamp rtPsNw = (PortableStamp)rtNw;
            
            Helper.WriteLine("Portable stamp normal way: [{0}].", normalWay);
            Helper.WriteLine("Pbf normal way: [{0}].", normalPbf);
            Helper.WriteLine("Pbs normal way: [{0}].", normalPbs);
            Helper.WriteLine("Pbf normal way RT: [{0}].", rtNw);
            Helper.WriteLine("Portable stamp normal way RT: [{0}].", rtPsNw);

            return (normalWay, normalPbf, normalPbs, rtNw, rtPsNw, alternateFromPbs, altPbfFromPbs, alternate);
        }

        [Fact]
        public void TestManyRandomRoundTrips()
        {
            PortableStamp pivot = new DateTime(1978, 6, 10, 16, 20, 04, DateTimeKind.Utc);
            pivot += PortableDuration.FromNanoseconds(123_456_789);
            const int numTests = 500;
            ExecutingRandomDeviationTest(numTests, in pivot);

        }

        private void ExecutingRandomDeviationTest(int numTests, in PortableStamp pivot)
        {
            const int minUpdateEvery = 10;
            StampConversionData temp = default;
            int updateEvery = numTests / 10 > minUpdateEvery ? numTests / 10 : minUpdateEvery;
            int updateCount = updateEvery;
            Helper.WriteLine("Going to execute {0:N0} tests around {1} pivot.", numTests, pivot.ToString());

            Assert.True(numTests > 0);
            RandomStampGenerator generator = new RandomStampGenerator(in pivot);
            Span<StampConversionData> conversionData = generator.CreateArrayOfStampConversionData(numTests);
            Helper.WriteLine($"All {numTests:N0} tests created.  Execution will begin.");

            int testIndex=-1;
            Exception? fault = null;
            MonotonicStamp? endedAt = null;
            MonotonicStamp startedAt = MonostampSrc.StampNow;
            try
            {
                ref StampConversionData data = ref temp;
                for (testIndex = 0; testIndex < conversionData.Length; ++testIndex)
                {
                    data = ref conversionData[testIndex];
                    ProtobufFormatStamp pbfFormat = (ProtobufFormatStamp)data.TestStamp;
                    PortableStamp rt = (PortableStamp)pbfFormat;
                    TestResult tr = new TestResult(in pbfFormat, in rt);
                    data.ProvideResultValue(in tr);
                    Assert.True(data.HasResult && data.ResultPasses);
                    if (--updateCount <= 0)
                    {
                        Helper.WriteLine("Completed {0:N0} tests of {1:N0} successfully...", testIndex+1, conversionData.Length);
                        updateCount = updateEvery;
                    }
                }

                endedAt = MonostampSrc.StampNow;
            }
            catch (Exception ex)
            {
                endedAt = MonostampSrc.StampNow;
                fault = ex;
                Helper.WriteLine(
                    $"Exception of type [{ex.GetType().Name}] with message \"{ex.Message}\" thrown while at test index [{testIndex}].  Tested value: [" +
                    (testIndex, conversionData.Length) switch
                    {
                        (var index, var count) when index < count && index > -1 => conversionData[index].TestStamp
                            .ToString(),
                        _ => "UNKNOWN: INDEXING LOGIC ERROR LIKELY"
                    } + "].");
                throw;
            }
            finally
            {
                MonotonicStamp ended = endedAt ?? MonostampSrc.StampNow;
                Duration elapsedTime = ended - startedAt;
                Helper.WriteLine("Test took {0:N6} milliseconds.", elapsedTime.TotalMilliseconds);
                if (fault == null)
                {
                    PortableDuration elapsed = (PortableDuration) elapsedTime;
                    PortableDuration perItem = elapsed / numTests;
                    Helper.WriteLine(
                        $"Over a series of {numTests} tests, on average, each test took {perItem.TotalNanoseconds:N0} nanoseconds.");
                }
            }
            
        }

        private void TestNanosecondsDeviationsAroundUnixEpoch(long nanosecondsToDeviate)
        {
            Assert.True(nanosecondsToDeviate > long.MinValue,
                "Can't be min value because can't negate 2's complement min value.");
            PortableDuration deviation = PortableDuration.FromNanoseconds(nanosecondsToDeviate);
            PortableDuration oppositeDeviation = -deviation;
            Helper.WriteLine(
                $"Testing deviation of {Math.Abs(nanosecondsToDeviate)} and {(Math.Abs(nanosecondsToDeviate))} nanoseconds from the unix epoch.");
            ref readonly PortableDuration negDev = ref oppositeDeviation;
            ref readonly PortableDuration posDev = ref deviation;
            if (deviation < PortableDuration.Zero)
            {
                negDev = ref deviation;
                posDev = ref oppositeDeviation;
            }

            PortableStamp positivelyDeviated = PortableStamp.UnixEpochStamp + posDev;
            string posDevTxt = positivelyDeviated.ToString();
            PortableStamp negativelyDeviated = PortableStamp.UnixEpochStamp + negDev;
            string negDevTxt = negativelyDeviated.ToString();
            PortableStamp positiveRoundTripped, negativeRoundTripped;


            Helper.WriteLine($"Testing POSITIVE deviation of {posDev.TotalNanoseconds:N0} nanoseconds (results in stamp: [{posDevTxt}]).");
            ProtobufFormatStamp pbfStamp = (ProtobufFormatStamp)positivelyDeviated;
            Helper.WriteLine($"POSITIVE Protobuf format -- (seconds: [{pbfStamp.Seconds:N0}]; nanoseconds: [{pbfStamp.Nanoseconds:N0}]).");
            positiveRoundTripped = (PortableStamp)pbfStamp;
            Helper.WriteLine("POSITIVE round tripped stamp: [{0}].", positiveRoundTripped.ToString());

            Helper.WriteLine($"Testing NEGATIVE deviation of {posDev.TotalNanoseconds:N0} nanoseconds (results in stamp: [{negDevTxt}]).");
            ProtobufFormatStamp negPbfStamp = (ProtobufFormatStamp)negativelyDeviated;
            Helper.WriteLine($"NEGATIVE Protobuf format -- (seconds: [{negPbfStamp.Seconds:N0}]; nanoseconds: [{negPbfStamp.Nanoseconds:N0}]).");
            negativeRoundTripped = (PortableStamp)negPbfStamp;
            Helper.WriteLine("NEGATIVE round tripped stamp: [{0}].", negativeRoundTripped.ToString());

            bool positivePasses = (positiveRoundTripped == positivelyDeviated &&
                                   positiveRoundTripped.GetHashCode() == positivelyDeviated.GetHashCode());
            bool negativePasses = (negativeRoundTripped == negativelyDeviated &&
                                   negativeRoundTripped.GetHashCode() == negativelyDeviated.GetHashCode());
            string message = (positivePasses, negativePasses) switch
            {
                (false, false) => "BOTH POSITIVE AND NEGATIVE TEST FAIL",
                (false, true) => "NEGATIVE TEST FAILS (positive test PASSES)",
                (true, false) => "POSITIVE TEST FAILS (negative test PASSES)",
                (true, true) => "BOTH POSITIVE AND NEGATIVE TEST PASS",
            };
            Assert.True(positivePasses && negativePasses, message);
            Helper.WriteLine(message);
        }
         
        private void TestPortableStampConversion(in PortableStamp stamp, string testName)
        {
            ProtobufFormatStamp? protobufFormatted = null;
            PortableStamp? rtFromProtFmt = null;
            try
            {
                Helper.WriteLine("Beginning \"{0}\" test starting with stamp: [{1}].", testName, stamp.ToString());
                protobufFormatted = (ProtobufFormatStamp)stamp;
                rtFromProtFmt = (PortableStamp)protobufFormatted;
                Assert.True(rtFromProtFmt == stamp && rtFromProtFmt.GetHashCode() == stamp.GetHashCode());
                Helper.WriteLine("Test \"{0}\" PASSED -- round tripped ok.", testName);
            }
            catch (XunitException ex)
            {
                Helper.WriteLine(
                    "\"{0}\" test FAILED because of exception [{1}]. ProtobufFormatted: [{2}]; Round tripped: [{3}].",
                    testName, ex, protobufFormatted?.ToString() ?? "UNAVAILABLE",
                    rtFromProtFmt?.ToString() ?? "UNAVAILABLE");
                throw;
            }

        }

       

       
    }

    readonly struct StampConversionData : IEquatable<StampConversionData>
    {
        public static void CreateConversionData(in PortableStamp basis, out StampConversionData result)
        {
            result = new StampConversionData(in basis);
        }

        public static StampConversionData[] CreateArrayOfStampConversionData(PortableStamp[] src)
        {
            Assert.NotNull(src);
            StampConversionData[] ret = new StampConversionData[src.Length];
            for (int i = 0; i < src.Length; ++i)
            {
                ref readonly PortableStamp fromSrc = ref src[i];
                CreateConversionData(in fromSrc, out ret[i]);
                Assert.False(ret[i].IsInvalidDefaultValue);
                Assert.False(ret[i].HasResult);
            }
            return ret;
        }

        public static ref readonly StampConversionData InvalidDefaultVal => ref TheInvalidDefaultValue;
        public bool IsInvalidDefaultValue => InvalidDefaultVal == this;
        public bool HasResult => _result.IsSet;
        public bool ResultPasses => HasResult && _result.Value.RoundTrippedValue == _startedAt;
        public bool ResultFails => HasResult && !ResultPasses;
        public PortableStamp TestStamp => _startedAt;
        public ref readonly TestResult Results => ref _result.Value;
        private StampConversionData(in PortableStamp testValue) => _startedAt = testValue;
        public void ProvideResultValue(in TestResult resultVal) => _result.SetValueOrThrow(in resultVal);

        public override int GetHashCode()
        {
            int hash = _startedAt.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ (HasResult ? _result.Value.GetHashCode() : 0);
            }
            return hash;
        }

        public override string ToString() => 
            $"[{nameof(StampConversionData)}] -- Test stamp: [{_startedAt}]; Result Data: [" +
            GetResultMessage() + "].";

        public static bool operator ==(in StampConversionData lhs, in StampConversionData rhs) =>
            lhs._startedAt == rhs._startedAt && lhs.HasResult == rhs.HasResult &&
            (!lhs.HasResult || lhs._result.Value == rhs._result.Value);
        public static bool operator!=(in StampConversionData lhs, in StampConversionData rhs) => 
            !(lhs== rhs);
        public bool Equals(StampConversionData other) => other == this;
        public override bool Equals(object? obj) => obj is StampConversionData scd && scd == this;

        private string GetResultMessage()
        {
            const string resultFrmtStr =
                "Round tripped stamp: [{0}]; Protobuf Stamp Seconds:[{1:N0}]; Protobuf Stamp Nanos:[{2:N0}].";
            ref readonly TestResult res = ref Results;
            return (HasResult, ResultPasses) switch
            {
                (false, _) => "NOT PROVIDED",
                (true, false) =>
                    "FAILING RESULT -- " + string.Format(resultFrmtStr, res.RoundTrippedValue.ToString(),
                        res.PbfStamp.Seconds, res.PbfStamp.Nanoseconds),
                (true, true) => "PASSING RESULT -- " + string.Format(resultFrmtStr, res.RoundTrippedValue.ToString(),
                    res.PbfStamp.Seconds, res.PbfStamp.Nanoseconds)
            };
        }

        private readonly PortableStamp _startedAt =
            (PortableStamp)MonostampSrc.StampNow;
        private readonly LocklessWriteOnceValue<TestResult> _result = new();
        private static readonly StampConversionData TheInvalidDefaultValue = default;
    }

    readonly struct TestResult : IEquatable<TestResult>
    {
        public PortableStamp RoundTrippedValue => _roundTrippedValue;
        public ProtobufFormatStamp PbfStamp => _pbfStamp;
        public ProtobufStamp FromPbf => _protostampFromPbf;
        public ProtobufStamp FromDt => _protostampFromDateTime;
        public bool ProtostampsEqual => _protobufStampsEqual;

        internal TestResult(in ProtobufFormatStamp pbfStamp, in PortableStamp roundTrippedStamp)
        {
            _pbfStamp = pbfStamp;
            _roundTrippedValue = roundTrippedStamp;
            _protostampFromPbf = new Timestamp{ Nanos = pbfStamp.Nanoseconds, Seconds = pbfStamp.Seconds };
            _protostampFromDateTime = Timestamp.FromDateTime((DateTime)roundTrippedStamp);
            int nanosZeroTo99 = _roundTrippedValue.FractionalSeconds % 99;
            _protostampFromDateTime += new Google.Protobuf.WellKnownTypes.Duration { Nanos = nanosZeroTo99 };
            _protobufStampsEqual = _protostampFromDateTime == _protostampFromPbf;
        }

        public override int GetHashCode()
        {
            int hash = _pbfStamp.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _roundTrippedValue.GetHashCode();
                hash = (hash * 397) ^ _protostampFromPbf.GetHashCode();
                hash = (hash * 397) ^ _protostampFromDateTime.GetHashCode();
                hash = (hash * 397) ^ _protobufStampsEqual.GetHashCode();
            }
            return hash;
        }

        public static bool operator ==(in TestResult lhs, in TestResult rhs) =>
            lhs._roundTrippedValue == rhs._roundTrippedValue && lhs._pbfStamp == rhs._pbfStamp &&
            lhs._protostampFromPbf == rhs._protostampFromPbf &&
            lhs._protostampFromDateTime == rhs._protostampFromDateTime &&
            lhs._protobufStampsEqual == rhs._protobufStampsEqual;
        public static bool operator !=(in TestResult lhs, in TestResult rhs) => !(lhs == rhs);
        public bool Equals(TestResult other) => other == this;
        public override bool Equals(object? obj) => obj is TestResult tr && tr == this;

        public override string ToString() =>
            $"[{nameof(TestResult)}] -- Round Tripped Stamp: [{_roundTrippedValue.ToString()}]; " +
            $"Pbf Seconds: [{_pbfStamp.Seconds}]; Pbf Nanos: [{_pbfStamp.Nanoseconds}]; " +
            $"Protostamp from pbf -- Seconds: [{_protostampFromPbf.Seconds}]; Nanos: [{_protostampFromPbf.Nanos}]; " +
            $"Protodtamp from date time -- Seconds: [{_protostampFromPbf.Seconds}]; Nanos: [{_protostampFromPbf.Nanos}].";


        private readonly PortableStamp _roundTrippedValue;  
        private readonly ProtobufFormatStamp _pbfStamp;
        private readonly ProtobufStamp _protostampFromPbf;
        private readonly ProtobufStamp _protostampFromDateTime;
        private readonly bool _protobufStampsEqual;
    }

    public readonly struct RandomStampGenerator
    {
        public PortableStamp PivotStamp => _pivot;
        private readonly Random RGen => s_theRGen ??= new Random();

        public RandomStampGenerator(in PortableStamp pivot) => _pivot = pivot;

        public PortableStamp NextStamp()
        {
            NextStamp(out PortableStamp ret);
            return ret;
        }

        internal StampConversionData[] CreateArrayOfStampConversionData(int count)
        {
            Assert.True(count > 0);
            StampConversionData[] ret = new StampConversionData[count];
            Span<PortableStamp> stamps = new PortableStamp[count];
            Fill(ref stamps);
            for (int i = 0; i < count; ++i)
            {
                StampConversionData.CreateConversionData(in stamps[i], out ret[i]);
            }
            Assert.DoesNotContain(ret, val => val.IsInvalidDefaultValue || val.HasResult);
            return ret;
        }

        private void Fill(ref Span<PortableStamp> fillWithRandom)
        {
            Random r = RGen;
            int addedSoFar = -1;
            while (++addedSoFar < fillWithRandom.Length)
            {
                NextStamp(out fillWithRandom[addedSoFar]);
            }
        }

        private void NextStamp(out PortableStamp result)
        {
            Random r = RGen;
            PortableDuration dev = GetRandomDeviation500YearMinMax(r);
            result = _pivot + dev;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PortableDuration GetRandomDeviation500YearMinMax(Random rgen)
        {
            const long secondsPerFiveHundredYears = 15_768_000_000L;
            const int exclusiveUpperBoundFracNanos = 1_000_000_000;
            //add extra second because upper bound is exclusive
            long val = rgen.NextInt64(0, secondsPerFiveHundredYears + 1);
            //(coin flip, note exclusive upper bound)
            bool makeNegative = rgen.Next(0, 2) == 1;
            if (makeNegative)
            {
                val = -val;
            }
            PortableDuration ret = PortableDuration.FromSeconds(val);
            int nanoSecondsModifier = rgen.Next(0, exclusiveUpperBoundFracNanos);
            if (makeNegative)
            {
                nanoSecondsModifier = -nanoSecondsModifier;
            }
            return ret;
        }


        private static PortableStamp InitDefaultStampValue()
        {
            PortableStamp stamp = new DateTime(1978, 6, 10, 16, 20, 04, DateTimeKind.Utc);
            return stamp + PortableDuration.FromNanoseconds(123_456_789);
        }

        [ThreadStatic] private static Random? s_theRGen;
        private readonly PortableStamp _pivot = InitDefaultStampValue();
    }
}
