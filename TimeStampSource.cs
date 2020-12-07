using System;
using ConfiguredUtil = HpTimeStamps.TimeStampUtil;
namespace HpTimeStamps
{
    /// <summary>
    /// Calibration is PER THREAD.  Calls are therefore thread-safe, but calibration may need to be done
    /// on each thread using this resource.
    /// </summary>
    /// <seealso cref="ConfiguredUtil">for more information</seealso>
    public static class TimeStampSource
    {
        /// <summary>
        /// True if high precision timestamps are used, false otherwise.
        /// </summary>
        public static bool IsHighPrecision => TheUtil.IsHighPrecision;
        /// <summary>
        /// High precision (if available) Utc timestamp
        /// </summary>
        public static DateTime UtcNow => TheUtil.CurrentUtcTimeStamp;
        /// <summary>
        /// High precision (if available) timestamp available here
        /// </summary>
        public static DateTime Now => TheUtil.CurrentLocalTimeStamp;
        /// <summary>
        /// True if calibration is needed on THIS thread, false otherwise
        /// </summary>
        public static bool NeedsCalibration => TheUtil.IsCalibrated;
        /// <summary>
        /// How long has it been since calibration (on THIS thread)
        /// </summary>
        public static TimeSpan TimeSinceCalibration => TheUtil.TimeSinceLastCalibration;
        /// <summary>
        /// The offset from utc.  To get utc time from local, SUBTRACT this value from local.
        /// To get local from utc, add this value to utc.
        /// </summary>
        public static TimeSpan LocalUtcOffset => TheUtil.LocalOffsetFromUtc;
        /// <summary>
        /// Perform calibration now for THIS thread
        /// </summary>
        public static void Calibrate()
        {
            TheUtil.Calibrate();
            //TheUtil.Calibrate();
        }

        private static readonly ConfiguredUtil TheUtil = new ConfiguredUtil();
    }
}