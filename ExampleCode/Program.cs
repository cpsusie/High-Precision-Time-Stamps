using System;
using HpTimeStamps;
using HpTimeStamp = System.DateTime;
using HpTsSource = HpTimeStamps.TimeStampSource;
using WallStamp = System.DateTime;
using MonotonicStampSource = HpTimeStamps.MonotonicTimeStampUtil<HpTimeStamps.MonotonicStampContext>;
using MonotonicStamp = HpTimeStamps.MonotonicTimeStamp<HpTimeStamps.MonotonicStampContext>;
namespace Timestamps
{
    
    
    
    
    public static class TimeStampSource
    {

        public static DateTime Now => MonoNow.ToLocalDateTime();
        public static DateTime UtcNow => MonoNow.ToLocalDateTime();
        public static MonotonicStamp MonoNow => MonotonicStampSource.StampNow;
        public static HpTimeStamp HpNow => HpTsSource.Now;
        public static HpTimeStamp HpUtcNow => HpTsSource.UtcNow;
        public static WallStamp WallNow => WallStamp.Now;
        public static WallStamp WallUtcNow => WallStamp.UtcNow;

        public static bool HpNeedsCalibration => HpTsSource.NeedsCalibration;
        public static TimeSpan TimeSinceLastCalibration => HpTsSource.TimeSinceCalibration;
        public static void CalibrateNow() => HpTsSource.Calibrate();
        public static ref readonly MonotonicStampContext MonotonicContext => ref MonotonicStampSource.StampContext;
    }
}

namespace ExampleCode
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Hello World, here is the monotonic context: {Timestamps.TimeStampSource.MonotonicContext}");
        }
    }
}
