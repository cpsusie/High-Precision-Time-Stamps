using System;
using HpTimeStamps;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace UnitTests
{
    public class PortableSerializationTests : FixtureAndTestOutHelperHavingTests<PortableSerializationTestFixture>
    {
        /// <inheritdoc />
        public PortableSerializationTests([NotNull] PortableSerializationTestFixture fixture,
            [NotNull] ITestOutputHelper helper) : base(fixture, helper) {}

        [Fact]
        public void TestRandomPair()
        {
            (PortableMonotonicStamp stamp, PortableDuration dur) = Fixture.RandomPair;
            TestRtSerDeser(in stamp);
            TestRtSerDeser(in dur);
        }

        [Fact]
        public void TestEdgeCases()
        {
            TestRtSerDeser(in PortableMonotonicStamp.MinValue);
            TestRtSerDeser(in PortableDuration.MinValue);

            string? nullTxt = null;

            Assert.Throws<ArgumentNullException>(() => PortableDuration.Parse(nullTxt));
            Assert.Throws<ArgumentNullException>(() => PortableMonotonicStamp.Parse(nullTxt));

            Assert.True(PortableDuration.TryParse(nullTxt) == null);
            Assert.True(PortableMonotonicStamp.TryParse(nullTxt) == null);

            string illegal = "Foobar";
            Assert.Throws<ArgumentException>(() => PortableDuration.Parse(illegal));
            Assert.Throws<InvalidPortableStampStringException>(() => PortableMonotonicStamp.Parse(illegal));

            Assert.True(PortableDuration.TryParse(illegal) == null);
            Assert.True(PortableMonotonicStamp.TryParse(illegal) == null);
        }

        [Fact]
        public void TestManyPairs()
        {
            const int numTests = 100_000;
            const int updateEvery = 10_000;
            int thisTest = 0;
            (PortableMonotonicStamp currentStamp, PortableDuration currentDuration) = Fixture.RandomPair;
            try
            {
                while (++thisTest <= numTests)
                {
                    if (thisTest % updateEvery == 0)
                    {
                        Helper.WriteLine("On test {0:N} of {1:N} with stamp [{2}] and duration [{3:N9} days]...",
                            thisTest, numTests, currentStamp.ToString(), currentDuration.TotalDays);
                    }
                    TestRtSerDeser(in currentStamp);
                    TestRtSerDeser(in currentDuration);
                    (currentStamp, currentDuration) = Fixture.RandomPair;
                }
            }
            catch (EqualException ex)
            {
                Helper.WriteLine(
                    "Test failed on test# {0:N} with stamp [{1}] and duration [{2:N9} days].  Exception: \"{3}\".",
                    thisTest, currentStamp.ToString(), currentDuration.TotalDays, ex);
                throw;
            }
        }

        private void TestRtSerDeser(in PortableDuration dur)
        {
            string serialized = dur.ToString();
            var rt = PortableDuration.Parse(serialized);
            if (dur != rt)
            {
                throw new EqualException(dur, rt);
            }
        }

        private void TestRtSerDeser(in PortableMonotonicStamp stamp)
        {
            string serialized = stamp.ToString();
            var rt = PortableMonotonicStamp.Parse(serialized);
            if (stamp != rt)
            {
                throw new EqualException(stamp, rt);
            }
        }
    }
}
