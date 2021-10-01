using System;
using System.Threading;
using HpTimeStamps;
using HpTimeStamps.BigMath;
using Xunit;
using MonotonicStampContext = HpTimeStamps.MonotonicStampContext;
namespace UnitTests
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicStampContext>;
    using MonotonicStampSource = MonotonicTimeStampUtil<MonotonicStampContext>;
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StampTestFixture
    {
        internal IByRoRefSerializerDeserializer<PortableMonotonicStamp> StampSerializerDeserializer { get; } = new PortableStampSerializer();
        internal IByRoRefSerializerDeserializer<PortableDuration> DurationSerializerDeserializer { get; } = new PortableDurationSerializer();
        public bool SevenDaysTicksFitsInLong => SevenDaysInDurationTicks <= long.MaxValue;
        public TimeSpan UtcLocalOffset => MonotonicStampFixture.UtcLocalOffset;
        public DateTime StampContextLocalBeginReference => MonotonicStampFixture.StampContextLocalBeginReference;
        public DateTime StampContextUtcBeginReference => MonotonicStampFixture.StampContextUtcBeginReference;
        public long StopwatchTickEquivalentToRefTime => MonotonicStampFixture.StopwatchTickEquivalentToRefTime;
        public long TimespanFrequency => MonotonicStampFixture.TimespanFrequency;
        
        public static ref readonly MonotonicStampContext StampContext =>
            ref MonotonicStampFixture.StampContext;

        public (MonotonicStamp StampNow, DateTime LocalEquivNow, MonotonicStamp StampWithOffset, DateTime DtWithOffset,  Duration RandomOffsetDuration, TimeSpan OffsetTs) RandomTimeWithin14DaysOfNow
        {
            get
            {
                var monotonicStamp = MonotonicStampNow;
                var localDt = monotonicStamp.ToLocalDateTime();
                Duration offset = RandomDurationNegSevenToPosSevenDays();
                TimeSpan tsOffset = (TimeSpan) offset;
                return (monotonicStamp, localDt, monotonicStamp + offset, localDt + tsOffset, offset, tsOffset);
            }
        }

        public MonotonicStamp MonotonicStampNow => TheMsFixture.StampNow;

        public PortableMonotonicStamp PortableStampNow => TheMsFixture.StampNow.ToPortableStamp();

        static StampTestFixture()
        {
            TheMsFixture = new MonotonicStampFixture();
            UInt128 maxTicks = ((UInt128) Duration.TicksPerDay) * 7;
            SevenDaysInDurationTicks = maxTicks;
        }

        private Duration RandomDurationNegSevenToPosSevenDays()
        {
            Span<byte> bytes = stackalloc byte[17];
            RGen.NextBytes(bytes);
            Span<byte> low = bytes.Slice(0, 8);
            Span<byte> high = bytes.Slice(8, 8);
            byte sign = bytes[16];
            UInt128 oneTwentyEight = new UInt128(BitConverter.ToUInt64(high), BitConverter.ToUInt64(low));
            oneTwentyEight %= (SevenDaysInDurationTicks + 1);
            Int128 ticks = new Int128(oneTwentyEight._hi, oneTwentyEight._lo);
            if (sign % 2 == 0)
                ticks = -ticks;
            var ret = new Duration(in ticks);
            Assert.True(ret.TotalDays <= 7.00001 && ret.TotalDays >= -7.00001);
            return ret;
        }

        private Random RGen => TheRGen.Value!;

        private static readonly MonotonicStampFixture TheMsFixture;
        private static readonly UInt128 SevenDaysInDurationTicks;
        private static readonly ThreadLocal<Random> TheRGen = new ThreadLocal<Random>(() => new Random(), false);
    }
}
