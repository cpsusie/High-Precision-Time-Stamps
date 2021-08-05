using System;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace UnitTests
{
    public abstract class TestOutputHelperHavingTests
    {
        [NotNull] public ITestOutputHelper Helper { get; }

        protected TestOutputHelperHavingTests([NotNull] ITestOutputHelper helper) =>
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
    }

    public abstract class FixtureAndTestOutHelperHavingTests<T> : TestOutputHelperHavingTests, IClassFixture<T> where T : class
    {

        [NotNull] public T Fixture { get; }

        protected FixtureAndTestOutHelperHavingTests([NotNull] T fixture, [NotNull] ITestOutputHelper helper) :
            base(helper) => Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    
}
