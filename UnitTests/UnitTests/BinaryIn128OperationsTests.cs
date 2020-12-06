using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HpTimeStamps.BigMath;
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
            Helper.WriteLine("Done test case operations.");
            Helper.WriteLine(string.Empty);
            Helper.WriteLine("Printing comparison edge cases: ");
            opNo = 0;
            foreach (ref readonly var op in Fixture.ComparisonEdgeCaseTests.AsSpan())
            {
                Helper.WriteLine("Operation#: {0}:\t\t{1}", ++opNo, op.ToString());
            }
            Helper.WriteLine("Done printing edge case comparison tests.");
        }

        [Fact]
        public void TestPrintMinValue()
        {
            string maxValue = Int128.MaxValue.ToString();
            string maxValueLessOne = (Int128.MaxValue - 1).ToString();
            string minValuePlusOne = (Int128.MinValue + 1).ToString();
            string minValue = Int128.MinValue.ToString();
            Assert.DoesNotContain(maxValue, itm => itm == ')' || itm == '(');
            Assert.DoesNotContain(maxValueLessOne, itm => itm == ')' || itm == '(');
            Assert.DoesNotContain(minValuePlusOne, itm => itm == ')' || itm == '(');
            Assert.DoesNotContain(minValue, itm => itm == ')' || itm == '(');
        }

        [Fact]
        public void TestComparisonEdgeCases()
        {
            int opNo = 0;
            foreach (ref readonly var op in Fixture.ComparisonEdgeCaseTests.AsSpan())
            {
                try
                {
                    ValidateOp(in op, ++opNo);
                }
                catch (Exception ex)
                {
                    Helper.WriteLine(ex.Message);
                    throw;
                }
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
                    ValidateOp(in op, ++opNo);
                }
                catch (Exception e)
                {
                    Helper.WriteLine(e.Message);
                    throw;
                }
            }
        }

        [Fact]
        public void ValidateOp1()
        {
            ValidateOp(in Fixture.TestCaseOneOperations.ItemRef(0), 1);
        }

        [Fact]
        public void ValidateOp2()
        {
            ValidateOp(in Fixture.TestCaseOneOperations.ItemRef(1), 2);
        }

        [Fact]
        public void ValidateOp3()
        {
            ValidateOp(in Fixture.TestCaseOneOperations.ItemRef(2), 3);
        }

        [Fact]
        public void ValidateOp4()
        {
            ValidateOp(in Fixture.TestCaseOneOperations.ItemRef(3), 4);
        }

        private void ValidateOp(in BinaryOperation bop, int opNo)
        {
            try
            {
                (bool validated, Int128 calculatedValue) = bop.Calculated_Value();
                Assert.True(validated,
                    $"Binary op# {opNo}: [{bop}] did not validate. Calculated value: [{calculatedValue}].\n " +
                    $"LOperand:\t[0x{bop.LeftOperand.ToString("X32")}].\n" +
                    $"ROperand:\t[0x{bop.RightOperand.ToString("X32")}].\n" +
                    $"ResultOp:\t[0x{bop.Result.ToString("X32")}].\n" +
                    $"CalcldOp:\t[0x{calculatedValue.ToString("X32")}].\n");
            }
            catch (Exception e)
            {
                Helper.WriteLine(e.Message); 
                throw;
            }
        }
    }
}
