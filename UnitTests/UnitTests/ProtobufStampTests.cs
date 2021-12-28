using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HpTimeStamps;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using ProtobufStamp = Google.Protobuf.WellKnownTypes.Timestamp;
using PortableStamp = HpTimeStamps.PortableMonotonicStamp;
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
        public void TestJpsEpoch()
        {
            PortableStamp testMe = new DateTime(1948, 11, 11, 11, 11, 11, DateTimeKind.Utc);
            testMe += PortableDuration.FromNanoseconds(11);
            TestPortableStampConversion(in testMe, "JPS_EPOCH");
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
}
