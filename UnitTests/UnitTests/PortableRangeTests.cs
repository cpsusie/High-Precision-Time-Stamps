using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HpTimeStamps;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;

namespace UnitTests
{
    using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<MonotonicContext>;
    using MonostampSrc = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>;

    public class PortableRangeTests : OutputHelperAndFixtureHavingTests<StampTestFixture>
    {
        /// <inheritdoc />
        public PortableRangeTests(ITestOutputHelper helper, StampTestFixture fixture) : base(helper, fixture)
        {
        }

        [Fact]
        public void TestPrintMaxPortableStamp()
        {
            MonotonicStamp startedAt = MonostampSrc.StampNow;
            ref readonly MonotonicContext context = ref startedAt.Context;
            string os = Environment.OSVersion.VersionString;
            bool isSixtyFourBit = Environment.Is64BitProcess;
            string introMsg = $"Begin test on operating system {os}, which" + (isSixtyFourBit ? " is " : " is not ") +
                              $"a 64-bit process.  Framework: \"{Environment.Version}\".";
            try
            {
                Helper.WriteLine(introMsg);
                Helper.WriteLine($"Monotonic ticks per second: [{context.TicksPerSecond:N}].");
                PortableMonotonicStamp max = PortableMonotonicStamp.MaxValue;
                Helper.WriteLine($"MAX portable monotonic stamp: [{max}].");
            }
            catch (PortableTimestampOverflowException ex)
            {
                Helper.WriteLine($"Test failed due to overflow.  Message: \"{ex.Message}\".  Ex contents: [{ex}].");
                throw;
            }
            catch (Exception ex)
            {
                Helper.WriteLine($"TEST FAILED DUE TO UNEXPECTED EXCEPTION: [{ex}].");
                  throw;
            }

            Helper.WriteLine("TEST PASSES");
        }
    }
}
