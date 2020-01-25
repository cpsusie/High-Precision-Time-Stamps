using System;

namespace HpTimesStamps
{
    /// <summary>
    /// Note that the times retrieved are not more accurate in ABSOLUTE terms.
    /// If <see cref="IsHighPrecision"/> successive calls to get timestamp will
    /// be more accurate (with respect to each other, within the <see cref="CalibrationWindow"/>).  
    /// </summary>
    public interface ITimeStampUtil
    {   
        /// <summary>
        /// Number of ticks per second by the stop watch's (and if <see cref="IsHighPrecision"/>, the HPET's)
        /// reckoning
        /// </summary>
        long StopWatchTicksPerSecond { get; }
        /// <summary>
        /// Number of ticks per second by DateTime and TimeSpan's reckoning
        /// </summary>
        long DateTimeTicksPerSecond { get; }
        /// <summary>
        /// Check whether high precision timestamps are possible, if not,
        /// might as well use DateTime.Now (unless you want hp wherever possible)
        /// </summary>
        bool IsHighPrecision { get; }
        /// <summary>
        /// Like DateTime.Now but based off of a reading from the high precision event timer
        /// and then converted into terms of the local time system
        /// </summary>
        DateTime CurrentLocalTimeStamp { get; }
        /// <summary>
        /// Like DateTime.UtcNow, but based off of a reading from the high precision event timer
        /// then converted into terms of utc time system
        /// </summary>
        DateTime CurrentUtcTimeStamp { get; }
        /// <summary>
        /// The amount of time calibration lasts for.
        /// </summary>
        TimeSpan CalibrationWindow { get; }
        /// <summary>
        /// Amount of time since last calibration.
        /// </summary>
        TimeSpan TimeSinceLastCalibration { get; }
        /// <summary>
        /// Whether the util is calibrated at present
        /// </summary>
        bool IsCalibrated { get; }
        /// <summary>
        /// Manually force a calibration.  If not calibrated, upon call to
        /// <see cref="CurrentLocalTimeStamp"/> or <see cref="CurrentUtcTimeStamp"/> calibration will
        /// be performed before TimeStamp retrieval causing a delay.  If you want to avoid that,
        /// calling this will do the trick.
        /// </summary>
        void Calibrate();
       
    }
}