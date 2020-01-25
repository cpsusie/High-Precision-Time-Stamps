using System;
using ConfiguredUtil = HpTimesStamps.TimeStampUtil;
namespace HpTimesStamps
{
    /// <summary>
    /// Calibration is PER THREAD.  Calls are therefore thread-safe, but calibration may need to be done
    /// on each thread using this resource.
    /// </summary>
    public static class TimeStampSource
    {
        public static bool IsHighPrecision => TheUtil.IsHighPrecision;
        public static DateTime UtcNow => TheUtil.CurrentUtcTimeStamp;
        public static DateTime Now => TheUtil.CurrentLocalTimeStamp;
        public static bool NeedsCalibration => TheUtil.IsCalibrated;
        public static TimeSpan TimeSinceCalibration => TheUtil.TimeSinceLastCalibration;
        public static void Calibrate()
        {
            TheUtil.Calibrate();
            //TheUtil.Calibrate();
        }

        private static readonly ConfiguredUtil TheUtil = new ConfiguredUtil();
    }
}