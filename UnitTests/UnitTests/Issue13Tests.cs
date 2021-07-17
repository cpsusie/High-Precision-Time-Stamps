using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
namespace UnitTests
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonotonicStampProvider = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
    public class Issue13Tests : FixtureAndTestOutHelperHavingTests<Issue13TextFixture>
    {
        public Issue13Tests([NotNull] ITestOutputHelper helper, 
            [NotNull] Issue13TextFixture fixture) : base(fixture, helper) {}

        //[Fact]
        //public void TryIt()
        //{
        //    GenerateAndSaveRoRefStuffToFile();
        //}

        [Fact]
        public void TestIdenticalConversionMethods()
        {
            Issue13StampTestPacket packet = Issue13StampTestPacket.CreateNewTestPacket();
            Helper.WriteLine("Testing packet [{0}]: ", packet);
            ValidateSamePortableStamps(in packet);
            Helper.WriteLine("Packet has same portable stamps.");
        }

        [Fact]
        public void TryDeser()
        {
            var testSource = from item in Fixture.AllTestPackages
                let title = item.Title
                let xml = item.XmlContents
                orderby title
                select (Title: title, TestPackage: DeserXmlStr(xml));
            int itemCount = 0;
            foreach (var item in testSource)
            {
                ++itemCount;
                Helper.WriteLine("Executing test named: {0} with {1} test packets:", item.Title,
                    item.TestPackage.Count);
                try
                {
                    Check(item.TestPackage, item.TestPackage.Count);
                    Helper.WriteLine("\tTest #{0} PASSED.", itemCount );
                    Helper.WriteLine("END TEST NAMED {0}", item.Title);
                }
                catch (Exception ex)
                {
                    Helper.WriteLine("\tItem #{0} FAILED!.  Exception msg: \"{1}\".", itemCount, ex.Message);
                    Helper.WriteLine("\tTest {0} FAILED", item.Title);
                    Helper.WriteLine("END TEST NAMED {0}", item.Title);
                    Helper.WriteLine(string.Empty);
                    throw;
                }
                Helper.WriteLine(string.Empty);
            }
        }

        private void Check(ByRefRoList<Issue13StampTestPacket> packets, int expectCount)
        {
            int expectedStampMatch = expectCount;
            int expectedStrMatch = expectCount;
            
            
            Assert.Equal(expectCount, packets.Count);

            int numCastEqualsToPortable = 0;
            int numStringsMatch = 0;

            foreach (ref readonly var item in packets)
            {
                if (item.CastEqualsToPortabled)
                    ++numCastEqualsToPortable;
                if (item.CastMatchesString && item.PortabledMatchesString)
                    ++numStringsMatch;
            }
            Helper.WriteLine("\tFirst deserialized item: [{0}].", packets[0]);
            Assert.Equal(expectedStampMatch, numCastEqualsToPortable);
            Assert.Equal(expectedStrMatch, numStringsMatch);

            
        }

        private void ValidateSamePortableStamps(in Issue13StampTestPacket packet)
        {
            Assert.False(packet == default);
            Assert.True(packet.PortableCastFromMonotonic == packet.PortableToPortabledFromMonotonic);
        }

        private ByRefRoList<Issue13StampTestPacket> DeserXmlStr(string xml)
        {
            ByRefRoList<Issue13StampTestPacket> packets;
            {
                using var stream = new MemoryStream();
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                DataContractSerializer deserializer = new DataContractSerializer(typeof(ByRefRoList<Issue13StampTestPacket>));
                packets = (ByRefRoList<Issue13StampTestPacket>)deserializer.ReadObject(stream);
            }
            Assert.NotNull(packets);
            Assert.True(packets.All(itm => itm != default));
            return packets;
        }

        // ReSharper disable once UnusedMember.Local -- used hwen TryIt uncommented
        private void GenerateAndSaveRoRefStuffToFile()
        {
            const int count = 100;
            var items = Issue13StampTestPacket.CreateManyTestPackets(count);
            Assert.NotNull(items);
            Assert.Equal(count, items.Count);
            Assert.True(items.All(it => it != default));

            int numCastEqualsToPortable = 0;
            int numStringsMatch = 0;

            foreach (ref readonly var item in items)
            {
                if (item.CastEqualsToPortabled)
                    ++numCastEqualsToPortable;
                if (item.CastMatchesString && item.PortabledMatchesString)
                    ++numStringsMatch;
            }

            FileInfo fi = GetStampedFileInfo();
            Helper.WriteLine($"Going to save {count} items to {fi.FullName}.");
            Helper.WriteLine("Count cast equals portable: {0}; Count strings match: {1}.", numCastEqualsToPortable, numStringsMatch);


            string xml;
            {
                using var memoryStream = new MemoryStream();
                using var streamReader = new StreamReader(memoryStream);
                DataContractSerializer serializer = new DataContractSerializer(typeof(ByRefRoList<Issue13StampTestPacket>));
                serializer.WriteObject(memoryStream, items);
                memoryStream.Position = 0;
                xml = streamReader.ReadToEnd();
            }
            {
                using StreamWriter sw = fi.CreateText();
                sw.Write(xml);
            }
            fi.Refresh();
            if (!fi.Exists) throw new FileNotFoundException("Could not confirm that wrote to file.", fi.FullName);
            Helper.WriteLine("Successfully wrote to file: [{0}].",  fi.FullName);
        }

        private FileInfo GetStampedFileInfo()
        {
            var stamp = MonotonicStampProvider.StampNow;
            ref readonly var context = ref stamp.Context;
            Helper.WriteLine("Ticks per second: {0}; Reference Time: {1:O}", context.TicksPerSecond, context.UtcDateTimeBeginReference);
            string pathAttemptNoExt = $"{SysName}_{stamp.ToString().Replace(':', '-')}";
            FileInfo testMe = new FileInfo(pathAttemptNoExt + Extension);
            const int max = 100;
            int tries = 0;
            while (testMe.Exists && ++tries < max)
            {
                testMe = new FileInfo(pathAttemptNoExt + $"_{tries}" + Extension);
            }

            if (testMe.Exists)
            {
                throw new InvalidOperationException("too many files with that name already.");
            }

            return testMe;
        }


        private const string SysName = "Win10x64_2_441_442_tps";
        //private const string SysName = "Win10x64_10_000_000_tps";
        private const string Extension = ".xml";
    }
}
