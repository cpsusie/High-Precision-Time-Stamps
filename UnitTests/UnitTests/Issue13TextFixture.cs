using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
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

        public ref readonly Issue13StampTestPacket Amzn2ToTemMilWinX64StrMismatch_1 =>
            ref TheStringificationMismatchAmzn2ToWinX64TenMil;

        public (string XmlContents, string Title) Windows10x64_2_441_442_Tps { get; } =
            (ReadXmlFromPath(TheWin10x64_2_441_442_tps_2021_07_17T19_44_52_7102099_04_00_XmlPath),
                "Windows 10 x64: 2,441,442 ticks per second.");
        
        public (string XmlContents, string Title) AmznLinux2_x64_1_000_000_000_Tps { get; } =
            (ReadXmlFromPath(TheAmznLinux2_1_000_000_000tps_2021_07_25_XmlPath),
                "Amazon Linux 2 x64:  1,000,000,000 ticks per second.");

        public (string XmlContents, string Title) Win10_x64_10_000_000_Tps { get; } =
            (ReadXmlFromPath(TheWin10x64_10_000_000tps__tps_2021_07_19T15_15_42_8424752_04_00_XmlPath),
                "Windows 10 x64:  10,000,000 ticks per second.");

        static Issue13TextFixture()
        {
            string stringError1Xml = ReadXmlFromPath(StringificationMismatchAmzn2ToWinX64TenMilXmlFile);
            TheStringificationMismatchAmzn2ToWinX64TenMil = Deser(stringError1Xml);

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
        
        private const string TheWin10x64_2_441_442_tps_2021_07_17T19_44_52_7102099_04_00_XmlPath =
            @"../../../Resources/Win10x64_2_441_442_tps_2021-07-17T19-44-52.7102099-04-00.xml";
        private const string TheAmznLinux2_1_000_000_000tps_2021_07_25_XmlPath =
            @"../../../Resources/AmznLnx2_1_000_000_000_tps_2021-07-25T13-58-11.8479424-04-00.xml";
        private const string TheWin10x64_10_000_000tps__tps_2021_07_19T15_15_42_8424752_04_00_XmlPath =
            @"../../../Resources/Win10x64_10_000_000_tps_2021-07-19T15-15-42.8424752-04-00.xml";


        private const string StringificationMismatchAmzn2ToWinX64TenMilXmlFile =
            @"../../../Resources/StringMismatch_2021-07-24T08-03-16.8566018-04-00.xml";

        private static readonly Issue13StampTestPacket TheStringificationMismatchAmzn2ToWinX64TenMil;
        //home/cpsusie/RiderProjects/High-Precision-Time-Stamps/UnitTests/UnitTests/bin/Debug/netcoreapp3.1/Resources
    }
}