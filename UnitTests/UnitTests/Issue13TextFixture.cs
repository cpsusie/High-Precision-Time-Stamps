using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace UnitTests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public sealed class Issue13TextFixture
    {
        [JetBrains.Annotations.NotNull]
        public IEnumerable<(string XmlContents, string Title)> AllTestPackages
        {
            get
            {
                yield return AmznLinux2_x64_1000000000_Tps;
                yield return Windows10x64_2441442_Tps;
            }
        }

        public (string XmlContents, string Title) Windows10x64_2441442_Tps { get; } =
            (ReadXmlFromPath(TheWin10x64_2_441_442_tps_2021_07_17T19_44_52_7102099_04_00_XmlPath),
                "Windows 10 x64: 2,441,442 ticks per second.");
        
        public (string XmlContents, string Title) AmznLinux2_x64_1000000000_Tps { get; } =
            (ReadXmlFromPath(TheAmznLinux2_1_000_000_000tps_2021_07_17T21_26_46_2535718_04_00_XmlPath),
                "Amazon Linux 2 x64:  1,000,000,000 ticks per second.");

        [JetBrains.Annotations.NotNull]
        private static string ReadXmlFromPath([JetBrains.Annotations.NotNull] string path)
        {
            FileInfo fi = new FileInfo(path);
            using var sr = fi.OpenText();
            return sr.ReadToEnd();
        }
        
        private const string TheWin10x64_2_441_442_tps_2021_07_17T19_44_52_7102099_04_00_XmlPath =
            @"../../../Resources/Win10x64_2_441_442_tps_2021-07-17T19-44-52.7102099-04-00.xml";
        private const string TheAmznLinux2_1_000_000_000tps_2021_07_17T21_26_46_2535718_04_00_XmlPath =
            @"../../../Resources/AmznLinux2_1_000_000_000tps_2021-07-17T21-26-46.2535718-04-00.xml";        
        
        ///home/cpsusie/RiderProjects/High-Precision-Time-Stamps/UnitTests/UnitTests/bin/Debug/netcoreapp3.1/Resources
    }
}