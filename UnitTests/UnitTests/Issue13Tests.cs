using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
namespace UnitTests
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonotonicStampProvider = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;
    public class Issue13Tests : TestOutputHelperHavingTests
    {
        /// <inheritdoc />
        public Issue13Tests([NotNull] ITestOutputHelper helper) : base(helper)
        {
        }

        [Fact]
        public void TryIt()
        {
            GenerateAndSaveRoRefStuffToFile();
        }

        [Fact]
        public void TryDeser()
        {
            FileInfo fi =
                new FileInfo(
                    @"Z:\Documents\repos\hp_timestamps\UnitTests\UnitTests\bin\Debug\netcoreapp3.1\Win10x64_10_000_000_tps_2021-07-17T13-33-21.1377508-04-00.xml");
            Assert.True(fi.Exists);
            RetrieveAndCheck(fi);
        }

        private void RetrieveAndCheck(FileInfo fi)
        {
            const int expectCount = 100;
            const int expectedStampMatch = 0;
            const int expectedStrMatch = 100;
            string xml;
            {
                using var sr = fi.OpenText();
                xml = sr.ReadToEnd();
            }
            ByRefRoList<Issue13StampTestPacket> packets; 
            {
                using var stream = new MemoryStream();
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                DataContractSerializer deserializer = new DataContractSerializer(typeof(ByRefRoList<Issue13StampTestPacket>));
                packets = (ByRefRoList < Issue13StampTestPacket >) deserializer.ReadObject(stream);
            }
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

            Assert.Equal(expectedStampMatch, numCastEqualsToPortable);
            Assert.Equal(expectedStrMatch, numStringsMatch);

            Helper.WriteLine("First deserialized item: [{0}].", packets[0]);
        }

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
            var context = stamp.Context;
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

        

        private const string SysName = "Win10x64_10_000_000_tps";
        private const string Extension = ".xml";
    }
}
