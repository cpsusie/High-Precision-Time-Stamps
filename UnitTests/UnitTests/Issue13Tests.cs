﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using HpTimeStamps;
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
            ValidateRoundTripMonostamp(in packet);
            Helper.WriteLine("Packet has same portable stamps.");
        }

        [Fact]
        public void Test_Amzn2ToTemMilWinX64StrMismatch_1()
        {
            ref readonly Issue13StampTestPacket packet = ref Fixture.Amzn2ToTemMilWinX64StrMismatch_1;
            string stringifiedHere = packet.PortableCastFromMonotonic.ToString();
            string stringifiedThere = packet.PortableCastFromMonotonicStringified;
            MonotonicStamp converted = (MonotonicStamp) packet.PortableCastFromMonotonic;
            string convertedTxt = converted.ToString();
            
            PortableMonotonicStamp roundTripped = (PortableMonotonicStamp) converted;

            Helper.WriteLine("Stringified here: {0}, stringified there: {1}", stringifiedHere, stringifiedThere);
            Helper.WriteLine("Deserialized portable cast from monotonic.  UTC: {0}, Local: {1}", packet.PortableCastFromMonotonic.ToString(), packet.PortableCastFromMonotonic.ToLocalString());
            Helper.WriteLine("Original monotonic string rep: {0}", packet.StringifiedMonotonicStamp);
            Helper.WriteLine("Converted from portable local monotonic: {0}", convertedTxt);
            Helper.WriteLine("Round tripped to portable - UTC: {0}, Local: {1}", roundTripped.ToString(), roundTripped.ToLocalString());
            Helper.WriteLine("Stringified versions of serialized portable: {0}", packet.PortableCastFromMonotonicStringified);

            Assert.Equal(roundTripped, packet.PortableCastFromMonotonic);
            Assert.True(string.Equals(stringifiedHere, packet.PortableCastFromMonotonicStringified, StringComparison.Ordinal));
            Assert.True(packet.CastMatchesString && packet.PortabledMatchesString);
        }

        [Fact]
        public void TryDeser()
        {
            var testSource = from item in Fixture.AllTestPackages
                let title = item.Title
                let xml = item.XmlContents
                select (Title: title, TestPackage: DeserXmlStr(xml));
            int itemCount = 0;
            foreach (var item in testSource)
            {
                ++itemCount;
                Helper.WriteLine("Executing test named: {0} with {1} test packets:", item.Title,
                    item.TestPackage.Count);
                try
                {
                    Check(item.TestPackage);
                    Helper.WriteLine("\tTest #{0} PASSED.", itemCount);
                    Helper.WriteLine("END TEST NAMED {0}", item.Title);
                }
                catch (PacketBearingException ex)
                {
                    string reason = ex switch
                    {
                        InterpretationOverflowedException {}=> "Overflow",
                        CastDoesNotEqualPortableException {}=> "CastPortableValueMismatch",
                        StringsDoNotMatchException {} => "StringMismatch",
                        _ => "Unknown"
                    };
                    Helper.WriteLine("Test failed due to {0}: {1}", reason, ex.Message);
                    string badPacketXml = PacketToXmlString(in ex.BadPacket);
                    FileInfo fi = null;
                    try
                    {
                        fi = GetFailureTargetInfo(reason);
                        {
                            using var sw = fi.CreateText();
                            sw.Write(badPacketXml);
                        }
                        fi.Refresh();
                        if (!fi.Exists)
                            throw new FileNotFoundException($"Unable to verify results saved to {fi.FullName}.");
                        Helper.WriteLine($"Wrote failing test to {fi.FullName}.");
                    }
                    catch (Exception ex2)
                    {
                        Helper.WriteLine($"Unable to verify bad packet saved to {fi?.FullName ?? "UNKNOWN"} file.  (Error msg: {ex2.Message}.) Will log instead: {Environment.NewLine}");
                        Helper.WriteLine(badPacketXml);
                    }

                    throw;

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

        private void ValidateRoundTripMonostamp(in Issue13StampTestPacket packet)
        {
            MonotonicStamp stamp = (MonotonicStamp) packet.PortableCastFromMonotonic;
            PortableMonotonicStamp rtPms = (PortableMonotonicStamp) stamp;
            Assert.True(rtPms == packet.PortableCastFromMonotonic);
            Assert.True(StringComparer.Ordinal.Equals(packet.StringifiedMonotonicStamp, stamp.ToString()));
        }
        
        
        private void Check(ByRefRoList<Issue13StampTestPacket> packets)
        {
            foreach (ref readonly var item in packets)
            {
                try
                {
                    if (!item.CastMatchesString || !item.PortabledMatchesString)
                        throw new StringsDoNotMatchException(in item);
                    else if (!item.CastEqualsToPortabled)
                        throw new CastDoesNotEqualPortableException(in item);
                }
                catch (PortableTimestampOverflowException ex)
                {
                    throw new InterpretationOverflowedException(in item, ex);
                }
            }
            
            Helper.WriteLine("\tFirst deserialized item: [{0}].", packets[0]);
        }

        private void ValidateSamePortableStamps(in Issue13StampTestPacket packet)
        {
            Assert.False(packet == default);
            Assert.True(packet.PortableCastFromMonotonic == packet.PortableToPortabledFromMonotonic);
        }

        private string PacketToXmlString(in Issue13StampTestPacket packet)
        {
            string xml;
            {
                using var memoryStream = new MemoryStream();
                using var streamReader = new StreamReader(memoryStream);
                DataContractSerializer serializer = new DataContractSerializer(typeof(Issue13StampTestPacket));
                serializer.WriteObject(memoryStream, packet);
                memoryStream.Position = 0;
                xml = streamReader.ReadToEnd();
            }
            return xml;
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

        private FileInfo GetFailureTargetInfo(string failureType)
        {
            var stamp = MonotonicStampProvider.StampNow;
            ref readonly var context = ref stamp.Context;
            Helper.WriteLine("Ticks per second: {0}; Reference Time: {1:O}", context.TicksPerSecond, context.UtcDateTimeBeginReference);
            string pathAttemptNoExt = $"{failureType}_{stamp.ToString().Replace(':', '-')}";
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

        //private const string SysName = "AmznLinux2_1_000_000_000tps";
        //private const string SysName = "Win10x64_2_441_442_tps";
        private const string SysName = "Win10x64_10_000_000_tps";
        private const string Extension = ".xml";
    }

    public sealed class CastDoesNotEqualPortableException : PacketBearingException
    {
        
        public CastDoesNotEqualPortableException(in Issue13StampTestPacket badPacket) :
            base(in badPacket, CreateMessage(in badPacket), null) {}

        private static string CreateMessage(in Issue13StampTestPacket badPacket)
            =>
                $"The specified packet has a different cast portable (cast portable: {badPacket.PortableCastFromMonotonic}) value from its conversion method value: {badPacket.PortableToPortabledFromMonotonic}.";

        
    }

    public sealed class StringsDoNotMatchException : PacketBearingException
    {
        public bool CastVersionMismatch => (_mismatchCode & MismatchedStrings.CastVersionDoesNotMatch) ==
                                           MismatchedStrings.CastVersionDoesNotMatch;

        public bool ToPortableDoesNotMatch => (_mismatchCode & MismatchedStrings.ToPortableVersionDoesNotMatch) ==
                                               MismatchedStrings.ToPortableVersionDoesNotMatch;

        public StringsDoNotMatchException(in Issue13StampTestPacket badPacket) : base(in badPacket, CreateMessage(in badPacket), null) =>
            _mismatchCode = ExtractMismatch(in badPacket);
        

        private static string CreateMessage(in Issue13StampTestPacket badPacket)
        {
            MismatchedStrings mismatch = ExtractMismatch(in badPacket);

            var context = MonotonicStampProvider.StampNow.Context;
            string baseMsg =
                $"The monotonic stamp acquired a source with {badPacket.AcqSysTicksPerSecond} ticks per " +
                $"seconds (string rep of original monotonic: {badPacket.StringifiedMonotonicStamp}) then " +
                $"converted to a portable stamp produces a different string representation in this environment " +
                $"than it did in its original environment (this environment's ticks per second: {context.TicksPerSecond}).  ";

            return baseMsg + mismatch switch
            {

                MismatchedStrings.BothMismatched => GetPortableMismatchString(in badPacket) + " " +
                                                    GetCastMismatchedString(in badPacket),
                MismatchedStrings.ToPortableVersionDoesNotMatch => GetPortableMismatchString(in badPacket),
                MismatchedStrings.CastVersionDoesNotMatch => GetCastMismatchedString(in badPacket),
                _ => "Unknown mismatch.",
            };

            static string GetPortableMismatchString(in Issue13StampTestPacket bp) =>
                $"The \"to portable\" timestamp when stringified at source equaled: [{bp.PortableToStampedFromMonotonicStringified}] but when stringified here equaled: [{bp.PortableToPortabledFromMonotonic}].";
            static string GetCastMismatchedString(in Issue13StampTestPacket bp2) => $"The \"CAST\" timestamp when stringified at source equaled: [{bp2.PortableCastFromMonotonicStringified}] but when stringified here equaled: [{bp2.PortableCastFromMonotonic}].";
            
        }

        static MismatchedStrings ExtractMismatch(in Issue13StampTestPacket packet) => (
                CastMismatch: !packet.CastMatchesString, ToPortableMismatch: !packet.PortabledMatchesString) switch
            {
                (true, true) => MismatchedStrings.BothMismatched,
                (true, false) => MismatchedStrings.CastVersionDoesNotMatch,
                (false, true) => MismatchedStrings.ToPortableVersionDoesNotMatch,
                _ => MismatchedStrings.NoMismatch
            };
        

        
        private readonly MismatchedStrings _mismatchCode;
    }

    public sealed class InterpretationOverflowedException : PacketBearingException
    {
        public ref readonly MonotonicContext InterpretativeContext => ref _context;

        public InterpretationOverflowedException(in Issue13StampTestPacket badPacket,
            PortableTimestampOverflowException inner) : base(in badPacket, CreateMessage(in badPacket, inner), inner)
        {
            _context = MonotonicStampProvider.StampNow.Context;
        }

        private static string CreateMessage(in Issue13StampTestPacket badPacket, PortableTimestampOverflowException inner)
        {
            return $"The packet provided caused overflow when checking it: {badPacket}.  Exception: [{inner}]";
        }

       
        private readonly MonotonicContext _context;
    }

    public abstract class PacketBearingException : ApplicationException
    {
        public ref readonly Issue13StampTestPacket BadPacket => ref _badPacket;

        protected PacketBearingException(in Issue13StampTestPacket badPacket, string msg, Exception inner) : base(msg,
            inner) => _badPacket = badPacket;

        private readonly Issue13StampTestPacket _badPacket;
    }

    [Flags]
    public enum MismatchedStrings : byte
    {
        NoMismatch = 0x00,
        CastVersionDoesNotMatch = 0x01,
        ToPortableVersionDoesNotMatch = 0x02,
        BothMismatched = CastVersionDoesNotMatch | ToPortableVersionDoesNotMatch
    }
}
