using System;
using System.Collections.Generic;
using System.Text;
using HpTimeStamps;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using MonotonicStampContext = HpTimeStamps.MonotonicStampContext;

namespace UnitTests
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicStampContext>;
    using MonotonicStampSource = MonotonicTimeStampUtil<MonotonicStampContext>;

    public  class PortableTests : OutputHelperAndFixtureHavingTests<PortableTestFixture>
    {
        public PortableTests([NotNull] ITestOutputHelper helper, [NotNull] PortableTestFixture fixture) : base(helper,
            fixture)
        {
        }
        [Fact]
        public void TestPrintMonotonicNow()
        {
            MonotonicStamp monotonicNow = Fixture.MonotonicStampNow;
            PortableMonotonicStamp now = Fixture.PortableStampNow;
            Helper.WriteLine("Portable stamp: {0}.", now);
            MonotonicStamp convertedToMonotonic = (MonotonicStamp) now;
            DateTime local = now.ToLocalDateTime();
            DateTime utc = now.ToUtcDateTime();
            Helper.WriteLine("Monotonic now: [{0}].", monotonicNow);
            Helper.WriteLine("Portable->monotonic: [{0}].", convertedToMonotonic);
            Helper.WriteLine("As local dt: [{0:O}].", local);
            Helper.WriteLine("As utc dt: [{0:O}.]", utc);

            var diff = convertedToMonotonic - monotonicNow;
            if (diff < Duration.Zero) diff = -diff;
            Helper.WriteLine("Difference in monotonic and portable -> monotonic: {0:N6} microseconds.", diff.TotalMicroseconds);
            Assert.True(diff < Duration.FromMilliseconds(1));

        }
    }
}
