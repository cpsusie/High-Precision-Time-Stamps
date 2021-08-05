using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using HpTimeStamps;
using Xunit;

namespace UnitTests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class Issue13TextFixture
    {
        [JetBrains.Annotations.NotNull]
        public IEnumerable<(string XmlContents, string Title)> AllTestPackages
        {
            get
            {
                yield return Win10_x64_10_000_000_Tps;
                yield return AmznLinux2_x64_1_000_000_000_Tps;
                yield return Windows10x64_2_441_442_Tps;
            }
        }

        public ImmutableArray<(string Input, DateTime ExpectedDt, int NanoSeconds)>
            DateTimeParseTestCasesArray => TheDtParseTestCasesArr.Value;
        public ref readonly Issue13StampTestPacket Amzn2ToTemMilWinX64StrMismatch_1 =>
            ref TheStringificationMismatchAmzn2ToWinX64TenMil;

        public (string XmlContents, string Title) Windows10x64_2_441_442_Tps { get; } =
            (ReadXmlFromPath(TheWin10x64_2_441_442_tps_2021_08_05T08_39_20_6886102_04_00_XmlPath),
                "Windows 10 x64: 2,441,442 ticks per second.");
        
        public (string XmlContents, string Title) AmznLinux2_x64_1_000_000_000_Tps { get; } =
            (ReadXmlFromPath(TheAmznLinux2_1_000_000_000tps_2021_08_05_XmlPath),
                "Amazon Linux 2 x64:  1,000,000,000 ticks per second.");

        public (string XmlContents, string Title) Win10_x64_10_000_000_Tps { get; } =
            (ReadXmlFromPath(TheWin10x64_10_000_000tps__tps_2021_08_05T08_20_07_0937719_04_00_XmlPath),
                "Windows 10 x64:  10,000,000 ticks per second.");

        public string SerializeStamp(PortableMonotonicStamp serializeMe)
        {
            string xml;
            {
                using var memoryStream = new MemoryStream();
                using var streamReader = new StreamReader(memoryStream);
                DataContractSerializer serializer = new DataContractSerializer(typeof(PortableMonotonicStamp));
                serializer.WriteObject(memoryStream, serializeMe);
                memoryStream.Position = 0;
                xml = streamReader.ReadToEnd();
            }
            return xml;
        }

        public PortableMonotonicStamp DeserializeStamp([JetBrains.Annotations.NotNull] string xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            PortableMonotonicStamp ret;
            {
                using var stream = new MemoryStream();
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                DataContractSerializer deserializer = new DataContractSerializer(typeof(PortableMonotonicStamp));
                ret = (PortableMonotonicStamp)deserializer.ReadObject(stream);
            }
            return ret;
        }

        static Issue13TextFixture()
        {
            var context = MonotonicTimeStampUtil<MonotonicStampContext>.StampNow.Context;
            Assert.False(context.IsInvalid);
            string stringError1Xml = ReadXmlFromPath(StringificationMismatchAmzn2ToWinX64TenMilXmlFile);
            TheStringificationMismatchAmzn2ToWinX64TenMil = Deser(stringError1Xml);
            TheDtParseTestCasesArr =
                new LocklessLazyWriteOnceValue<ImmutableArray<(string Input, DateTime ExpectedDt, int NanoSeconds)>>(
                    () => SourceForPtsDtParse.ToImmutableArray());
        }

        [JetBrains.Annotations.NotNull]
        private static string ReadXmlFromPath([JetBrains.Annotations.NotNull] string path)
        {
            FileInfo fi = new FileInfo(path);
            using var sr = fi.OpenText();
            return sr.ReadToEnd();
        }

        private static Issue13StampTestPacket Deser(string xml)
        {
            Issue13StampTestPacket packet;
            {
                using var stream = new MemoryStream();
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                DataContractSerializer deserializer = new DataContractSerializer(typeof(Issue13StampTestPacket));
                packet = (Issue13StampTestPacket)deserializer.ReadObject(stream);
            }
            Assert.False(packet == default);
            return packet;
        }

        static IEnumerable<(string Input, DateTime ExpectedDt, int NanoSeconds)> SourceForPtsDtParse
        {
            get
            {
                yield return ("2021-07-25T17:57:51.0842084Z", new DateTime(2021, 7, 25, 17, 57, 51, DateTimeKind.Utc),
                    84_208_400);
                yield return ("2021-07-25T17:57:51Z", new DateTime(2021, 7, 25, 17, 57, 51, DateTimeKind.Utc),
                    0);
                yield return ("2021-07-25T17:57:51.1Z", new DateTime(2021, 7, 25, 17, 57, 51, DateTimeKind.Utc),
                    100_000_000);
                yield return ("2021-07-25T17:57:51.000000001Z", new DateTime(2021, 7, 25, 17, 57, 51, DateTimeKind.Utc),
                    1);
                yield return ("2021-07-25T17:57:51.10Z", new DateTime(2021, 7, 25, 17, 57, 51, DateTimeKind.Utc),
                    100_000_000);
                yield return ("2021-07-25T17:57:51.084208412Z", new DateTime(2021, 7, 25, 17, 57, 51, DateTimeKind.Utc),
                    84_208_412);
                yield return ("2021-07-25T17:57:51.984208412Z", new DateTime(2021, 7, 25, 17, 57, 51, DateTimeKind.Utc),
                    984_208_412);
                yield return ("2021-07-19T19:15:42.8303008Z", new DateTime(2021, 7, 19, 19, 15, 42, DateTimeKind.Utc),
                    830_300_800);
                yield return ("2021-07-17T23:44:52.702897851Z", new DateTime(2021, 7, 17, 23, 44, 52, DateTimeKind.Utc),
                    702_897_851);
            }
        }

        [JetBrains.Annotations.NotNull] private static readonly
            LocklessLazyWriteOnceValue<ImmutableArray<(string Input, DateTime ExpectedDt, int NanoSeconds)>>
            TheDtParseTestCasesArr;
        private const string TheWin10x64_2_441_442_tps_2021_08_05T08_39_20_6886102_04_00_XmlPath =
            @"../../../Resources/Win10x64_2_441_442_tps_2021-08-05T08-39-20.6866102-04-00.xml";
        private const string TheAmznLinux2_1_000_000_000tps_2021_08_05_XmlPath =
            @"../../../Resources/AmznLinux2_1_000_000_000tps_2021-08-05T09-06-50.3661107-04-00.xml";
        private const string TheWin10x64_10_000_000tps__tps_2021_08_05T08_20_07_0937719_04_00_XmlPath =
            @"../../../Resources/Win10x64_10_000_000_tps_2021-08-05T08-20-07.0937719-04-00.xml";


        private const string StringificationMismatchAmzn2ToWinX64TenMilXmlFile =
            @"../../../Resources/StringMismatch_2021-07-24T08-03-16.8566018-04-00.xml";

        private static readonly Issue13StampTestPacket TheStringificationMismatchAmzn2ToWinX64TenMil;
        //home/cpsusie/RiderProjects/High-Precision-Time-Stamps/UnitTests/UnitTests/bin/Debug/netcoreapp3.1/Resources
    }
}