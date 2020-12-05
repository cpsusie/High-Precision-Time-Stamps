using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public class BinaryIn128OperationsTests : OutputHelperAndFixtureHavingTests<BinaryOperationFixture>
    {
        public BinaryIn128OperationsTests([NotNull] ITestOutputHelper helper, [NotNull] BinaryOperationFixture fixture)
            : base(helper, fixture) { }
        
        [Fact]
        public void TestInitializations()
        {
            int opNo = 0;
            Assert.Equal(4, Fixture.TestCaseOneOperations.Length);
            foreach (ref readonly var op in Fixture.TestCaseOneOperations.AsSpan())
            {
                Helper.WriteLine("Operation#: {0}:\t\t{1}", ++opNo, op.ToString());
            }
        }

        [Fact]
        public void ValidateAllTestCaseOneOperations()
        {
            int opNo = 0;
            Assert.Equal(4, Fixture.TestCaseOneOperations.Length);
            foreach (ref readonly var op in Fixture.TestCaseOneOperations.AsSpan())
            {
                
                try
                {
                    Assert.True(op.Validate(), $"op: {op} did not validate.");
                }
                catch (Exception e)
                {
                    Helper.WriteLine(e.Message);
                    throw;
                }
            }
        }
    }
}
