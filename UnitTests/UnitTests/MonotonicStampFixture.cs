using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using HpTimeStamps;
using JetBrains.Annotations;
using Xunit;
using MonotonicStampContext = HpTimeStamps.MonotonicStampContext;
namespace UnitTests
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicStampContext>;
    using MonotonicStampSource = MonotonicTimeStampUtil<MonotonicStampContext>;
    public class MonotonicStampFixture
    {
        internal IByRoRefSerializerDeserializer<PortableMonotonicStamp> StampSerializerDeserializer { get; } = new PortableStampSerializer();
        internal IByRoRefSerializerDeserializer<PortableDuration> DurationSerializerDeserializer { get; } = new PortableDurationSerializer();
        public static TimeSpan UtcLocalOffset { get; }
        public static DateTime StampContextLocalBeginReference { get; }
        public static DateTime StampContextUtcBeginReference { get; }
        public static long StopwatchTickEquivalentToRefTime { get; }
        public static long TickFrequency { get; }
        public static long TimespanFrequency { get; }

        public static ref readonly MonotonicStampContext StampContext =>
            ref MonotonicTimeStampUtil<MonotonicStampContext>.StampContext;
        public DateTime Now => MonotonicStampSource.Now;
        public DateTime UtcNow => MonotonicStampSource.UtcNow;
        public MonotonicStamp StampNow => MonotonicStampSource.StampNow;

        static MonotonicStampFixture()
        {
            if (!MonotonicStampSource.IsContextSet)
            {
                var temp = MonotonicStampSource.StampContext;
                Assert.False(temp.IsInvalid);
                Assert.True(MonotonicStampSource.IsContextSet);
            }
            
            StampContextLocalBeginReference = StampContext.LocalTimeBeginReference;
            StampContextUtcBeginReference = StampContext.UtcDateTimeBeginReference;
            StopwatchTickEquivalentToRefTime = StampContext.ReferenceTicks;
            UtcLocalOffset = StampContext.UtcLocalTimeOffset;
            TickFrequency = StampContext.TicksPerSecond;
            TimespanFrequency = TimeSpan.TicksPerSecond;
        }

    }

    interface IByRoRefSerializerDeserializer<T> : ISerializerDeserializer<T> where T : struct
    {
        string SerializeToString(in T serializeMe);
    }

    interface ISerializerDeserializer<T>
    {
        string SerializeToString(T serializeMe);

        T DeserializeFromString(string deserializeMe);
    }

    internal readonly struct PortableStampSerializer : IByRoRefSerializerDeserializer<PortableMonotonicStamp>
    {
        public string SerializeToString(in PortableMonotonicStamp serializeMe)
        {
            using var output = new StringWriter();
            using var writer = new XmlTextWriter(output) { Formatting = Formatting.Indented };
            TheDcSerializer.WriteObject(writer, serializeMe);
            return output.GetStringBuilder().ToString();
        }

        string ISerializerDeserializer<PortableMonotonicStamp>.SerializeToString(PortableMonotonicStamp stamp) =>
            SerializeToString(in stamp);

        public PortableMonotonicStamp DeserializeFromString(string xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            using (Stream stream = new MemoryStream())
            {

                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                object? obj = TheDcSerializer.ReadObject(stream);
                return obj switch
                {
                    null => throw new SerializationException("Deserializer returned a null reference."),
                    PortableMonotonicStamp m => m,
                    {} o => throw new SerializationException($"Received value ({o}) of type ({o.GetType().Name}) when expected type was {nameof(PortableMonotonicStamp)}."),
                };
            }
        }



        private static readonly DataContractSerializer TheDcSerializer = new DataContractSerializer(typeof(PortableMonotonicStamp));
    }

    internal readonly struct PortableDurationSerializer : IByRoRefSerializerDeserializer<PortableDuration>
    {
        public string SerializeToString(in PortableDuration serializeMe)
        {
            using var output = new StringWriter();
            using var writer = new XmlTextWriter(output) { Formatting = Formatting.Indented };
            TheDcSerializer.WriteObject(writer, serializeMe);
            return output.GetStringBuilder().ToString();
        }

        string ISerializerDeserializer<PortableDuration>.SerializeToString(PortableDuration stamp) =>
            SerializeToString(in stamp);

        public PortableDuration DeserializeFromString(string xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            using (Stream stream = new MemoryStream())
            {

                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                object? obj = TheDcSerializer.ReadObject(stream);
                return obj switch
                {
                    null => throw new SerializationException("Deserializer returned a null reference."),
                    PortableDuration d => d,
                    { } o => throw new SerializationException($"Received value ({o}) of type ({o.GetType().Name}) when expected type was {nameof(PortableDuration)}."),
                };
            }
        }
        
        private static readonly DataContractSerializer TheDcSerializer = new DataContractSerializer(typeof(PortableDuration));
    }
}
