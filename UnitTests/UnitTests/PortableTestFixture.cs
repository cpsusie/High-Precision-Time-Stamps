using System;
using System.Collections.Generic;
using System.Text;
using HpTimeStamps;
using JetBrains.Annotations;
using MonotonicStampContext = HpTimeStamps.MonotonicStampContext;
namespace UnitTests
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicStampContext>;
    using MonotonicStampSource = MonotonicTimeStampUtil<MonotonicStampContext>;
    public class PortableTestFixture
    {
        internal IByRoRefSerializerDeserializer<PortableMonotonicStamp> StampSerializerDeserializer { get; } = new PortableStampSerializer();
        internal IByRoRefSerializerDeserializer<PortableDuration> DurationSerializerDeserializer { get; } = new PortableDurationSerializer();
        public TimeSpan UtcLocalOffset => MonotonicStampFixture.UtcLocalOffset;
        public DateTime StampContextLocalBeginReference => MonotonicStampFixture.StampContextLocalBeginReference;
        public DateTime StampContextUtcBeginReference => MonotonicStampFixture.StampContextUtcBeginReference;
        public long StopwatchTickEquivalentToRefTime => MonotonicStampFixture.StopwatchTickEquivalentToRefTime;
        public long TimespanFrequency => MonotonicStampFixture.TimespanFrequency;

        public static ref readonly MonotonicStampContext StampContext =>
            ref MonotonicStampFixture.StampContext;

        public MonotonicStamp MonotonicStampNow => TheMsFixture.StampNow;

        public PortableMonotonicStamp PortableStampNow => MonotonicStampNow;

        static PortableTestFixture() => TheMsFixture = new MonotonicStampFixture();

        [NotNull] private static readonly MonotonicStampFixture TheMsFixture;
    }
}
