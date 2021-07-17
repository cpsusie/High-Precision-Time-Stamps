using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnitTests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class Issue13TextFixture
    {
        [JetBrains.Annotations.NotNull]
        public IEnumerable<(string XmlContents, string Title)> AllTestPackages
        {
            get { yield return Windows10x64_2441442_Tps; }
        }

        public (string XmlContents, string Title) Windows10x64_2441442_Tps { get; } =
            (Issue12Resources.Win10x64_2_441_442_tps_2021_07_17T17_53_59_6774022_04_00,
                "Windows 10 x64: 2,441,442 ticks per second.");
    }
}