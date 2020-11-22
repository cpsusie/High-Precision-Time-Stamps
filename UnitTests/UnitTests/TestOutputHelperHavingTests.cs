using System;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace UnitTests
{
    public abstract class TestOutputHelperHavingTests
    {
        [NotNull] public ITestOutputHelper Helper { get; }

        protected TestOutputHelperHavingTests([NotNull] ITestOutputHelper helper) =>
            Helper = helper ?? throw new ArgumentNullException(nameof(helper));
    }
}
