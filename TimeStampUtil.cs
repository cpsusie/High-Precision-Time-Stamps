using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace HpTimesStamps
{
    /// <summary>
    /// This struct is used in <see cref="TimeStampSource"/> to provide high precision timestamps on a per thread basis.
    /// These timestamps have exactly the same format as timestamps retrieved by <see cref="DateTime.Now"/>, which
    /// corresponds to <see cref="CurrentLocalTimeStamp"/> herein and to <see cref="TimeStampSource.Now"/> and <see cref="DateTime.UtcNow"/>,
    /// which corresponds to <see cref="CurrentUtcTimeStamp"/> herein and to <see cref="TimeStampSource.UtcNow"/>.
    ///
    /// Please note that this (where hardware makes it possible) provides high PRECISION timestamps, not necessarily high ACCURACY
    /// timestamps.  DateTime.Now or .UtcNow may actually provide timestamps that are more accurate in terms of lack
    /// of deviation from official times.  These timestamps, however, should be -- on a single thread at least -- more accurate with respect
    /// to successively retrieved timestamps within a rather short period of time because they, where possible, make use of the hardware's high precision
    /// event timer.
    ///
    /// Calibration occurs on a per-thread basis by retrieving establishing an offset between the ticks value of DateTime.Now and the
    /// high precision event timer.  It then converts subsequent readings from the high precision timer back into DateTime format.
    ///
    /// As the time since <see cref="TimeSinceLastCalibration"/> increases, the high precision timer becomes LESS ACCURATE vis-a-vis
    /// "absolute" real time, but remains accurate with respect to the elapsed time the last time stamp was retrieved, a short time ago.
    /// Every time <see cref="TimeSinceLastCalibration"/> elapses, the next time stamp retrieval will recalibrate, causing retrieval to
    /// take slightly longer.  You can call <see cref="Calibrate"/> manually to do this yourself immediately before retrieving timestamps.
    ///
    /// The use-case for these timestamps is when you need timestamps that are approximately accurate vis-a-vis the system clock but also
    /// occasionally use them to measure elapsed time between events that occur in fractions of a millisecond.  In the latter use, they should
    /// be far better suited than DateTime.Now.  They may be somewhat less accurate than DateTime.Now with respect to the absolute, official
    /// UTC time, but remain accurate enough for many such purposes.
    ///
    /// Note that this library may malfunction until recalibration if the system clock changes ... at least until recalibration occurs.
    ///
    /// This struct is a time stamp util.  It should be accessed via TimeStampSource.
    /// If you wish to make your own time stamp util struct, make sure it has all the same public
    /// methods, operators and properties that this struct has.  Then alter the alias at the top of
    /// <see cref="TimeStampSource"/>file to use yours and then recompile.  I do not recommend using interface
    /// as that introduces unneeded delay.
    ///
    /// The time stamp state is all thread local.  This means that calibration is done on a per thread basis.
    /// 
    /// </summary>
    [SuppressMessage("ReSharper", "LocalizableElement")]
    public readonly struct TimeStampUtil : ITimeStampUtil, IEquatable<TimeStampUtil>, IEquatable<ITimeStampUtil>
    {
        /// <summary>
        /// True if a high precision event timer is available, false otherwise
        /// </summary>
        public bool IsHighPrecision => TheSwIsHighPrec;
        /// <summary>
        /// True if the util has a current calibration.  <seealso cref="CalibrationWindow"/> for how long
        /// calibration lasts before becoming considered stale.
        /// </summary>
        public bool IsCalibrated => TheSIsCalibrated.Value && TimeSinceLastCalibration < TheMaxTimeBeforeRecalibration;
        /// <summary>
        /// How many high precision ticks per second are there?
        /// </summary>
        public long StopWatchTicksPerSecond => TheStopWatchTicksPerSecond;
        /// <summary>
        /// How many date time ticks per second are there?
        /// </summary>
        public long DateTimeTicksPerSecond => TheDateTimeTicksPerSecond;
        /// <summary>
        /// How much time has elapsed since last calibration (per thread)
        /// </summary>
        public TimeSpan TimeSinceLastCalibration => DateTime.Now - TheSLastCalibTime.Value;
        /// <summary>
        /// How much time may elapse before calibration becomes considered stale.
        /// </summary>
        public TimeSpan CalibrationWindow => TheMaxTimeBeforeRecalibration;
        /// <summary>
        /// Not yet tested; probably will not work.
        /// todo fixit make it work
        /// </summary>
        public DateTime CurrentUtcTimeStamp
        {
            get
            {
                long ts = Stopwatch.GetTimestamp();
                long differential = TheSDtDifferential.Value;
                if (!IsCalibrated)
                {
                    Calibrate();
                    ts = Stopwatch.GetTimestamp();
                    differential = TheSDtDifferential.Value;
                }
                return new DateTime(ConvertStopwatchTicksToDateTimeTicks(ts, differential), DateTimeKind.Utc);
            }
        }

        /// <summary>
        /// Analog for <seealso cref="DateTime.Now"/> uses higher precision
        /// </summary>
        public DateTime CurrentLocalTimeStamp
        {
            get
            {
                long ts = Stopwatch.GetTimestamp();
                long differential = TheSDtDifferential.Value;
                if (!IsCalibrated)
                {
                    Calibrate();
                    ts = Stopwatch.GetTimestamp();
                    differential = TheSDtDifferential.Value;
                }
                return new DateTime(ConvertStopwatchTicksToDateTimeTicks(ts, differential), DateTimeKind.Local);
            }
        }

        /// <summary>
        /// Perform calibration
        /// </summary>
        public void Calibrate() => DoCalibrate();
        /// <summary>
        /// Always true ... all state is thread local and static
        /// </summary>
        /// <returns>true</returns>
        public static bool operator ==(TimeStampUtil lhs, TimeStampUtil rhs) => true;
        /// <summary>
        /// Always false, all state is thread local and static
        /// </summary>
        /// <returns>false</returns>
        public static bool operator !=(TimeStampUtil lhs, TimeStampUtil rhs) => !(lhs == rhs);
        /// <summary>
        /// Always true ... all state is thread local and static
        /// </summary>
        /// <returns>true</returns>
        public bool Equals(TimeStampUtil other) => true;
        /// <summary>
        /// 397 is always the hash code.  All instances are considered equal
        /// </summary>
        /// <returns>397</returns>
        public override int GetHashCode() => 397;
        /// <summary>
        /// True if <paramref name="other"/> is a not null and is of type <see cref="TimeStampUtil"/>,
        /// false otherwise.
        /// </summary>
        /// <param name="other">Another object implementing this interface</param>
        /// <returns>True if <paramref name="other"/> is a not null and is of type <see cref="TimeStampUtil"/>,
        /// false otherwise.</returns>
        public bool Equals(ITimeStampUtil other) => other as TimeStampUtil? == this;
        /// <summary>
        /// True if <paramref name="other"/> is a not null and is of type <see cref="TimeStampUtil"/>,
        /// false otherwise.
        /// </summary>
        /// <param name="other">Another object implementing this interface</param>
        /// <returns>True if <paramref name="other"/> is a not null and is of type <see cref="TimeStampUtil"/>,
        /// false otherwise.</returns>
        public override bool Equals(object other) => other as TimeStampUtil? == this;

        static TimeStampUtil()
        {
            TheSwIsHighPrec = Stopwatch.IsHighResolution;
            TheStopWatchTicksPerSecond  = Stopwatch.Frequency;
            TheDateTimeTicksPerSecond = TimeSpan.TicksPerSecond;
            if (TheStopWatchTicksPerSecond == TheDateTimeTicksPerSecond)
                TheStopWatchTicksToDateTimeTicksConversionFactor = null;
            else
            {
                TheStopWatchTicksToDateTimeTicksConversionFactor =
                    1.0 / ((double) TheStopWatchTicksPerSecond / TheDateTimeTicksPerSecond);
                
            }
        }

        private void DoCalibrate()
        {
            long differential = ComputeDifferential();
            TheSIsCalibrated.Value = true;
            TheSDtDifferential.Value = differential;
            TheSLastCalibTime.Value = DateTime.Now;
        }

        private static long ComputeDifferential()
        {
            List<long> differences = new List<long>(NumberOfCalibrationSamples);
            while (differences.Count < NumberOfCalibrationSamples)
            {
                long tsDt = DateTime.Now.Ticks;
                long convertedSwTicks = ConvertStopwatchTicksToDateTimeTicks(Stopwatch.GetTimestamp());
                differences.Add(tsDt - convertedSwTicks);
            }
            LogDifferences(differences);
            return Convert.ToInt64(differences.Min());
        }

        [Conditional("TRACE_TIMESTAMPS")]
        private static void LogDifferences(List<long> diffs)
        {
            Debug.Assert(diffs.Count > 1 && diffs.Count == NumberOfCalibrationSamples);

            IList<long> sortedDiffs = diffs.OrderBy(l => l).ToArray();
            long min = sortedDiffs.First();
            long max = sortedDiffs.Last();
            long range = Math.Abs(max - min);
            long average = Convert.ToInt64(diffs.Average());
            long median;
            bool evenCount = diffs.Count % 2 == 0;
            if (evenCount)
            {
                if (sortedDiffs.Count == 2)
                    median = average;
                else
                {
                    int secondMidIndex = diffs.Count / 2;
                    int firstMidIndex = secondMidIndex - 1;
                    median = (sortedDiffs[firstMidIndex] + sortedDiffs[secondMidIndex]) / 2;
                }
            }
            else
            {
                median = sortedDiffs[diffs.Count / 2];
            }

            int sampleNum = 0;
            Console.WriteLine("Writing all {0} samples: ", diffs.Count.ToString());
            foreach (var l in diffs)
            {
                Console.WriteLine("\t# {0}:\t\t\t{1:N}", (++sampleNum).ToString(), l);
            }
            Console.WriteLine("Logging difference statistics:");
            Console.WriteLine("\tOrdering detected: [{0}]", EvaluateUnsortedSortedOrder(diffs, sortedDiffs));
            Console.WriteLine("\t#Samples: {0}", sortedDiffs.Count);
            Console.WriteLine("\tMin: {0:N}", min);
            Console.WriteLine("\tMax: {0:N}", max);
            Console.WriteLine("\tRange: {0:N}", range);
            Console.WriteLine("\tAvg: {0:N}", average);
            Console.WriteLine("\tMedian: {0:N}", median);

            string EvaluateUnsortedSortedOrder(IList<long> unsorted, IList<long> sorted)
            {
                Debug.Assert(unsorted.Count == sorted.Count && unsorted.Count > 1);
                if (unsorted.SequenceEqual(sorted)) return "The differentials from sampling were in ascending order.";
                if (unsorted.SequenceEqual(sorted.Reverse())) return "The differentials from sample were in descending order.";
                return "The differentials were not in any recognized order.";
            }

        }


        private static long ConvertStopwatchTicksToDateTimeTicks(long swTicks, long differential) =>
            ConvertStopwatchTicksToDateTimeTicks(swTicks) + differential;
        private static long ConvertStopwatchTicksToDateTimeTicks(long swTicks) =>
            TheStopWatchTicksToDateTimeTicksConversionFactor == null
                ? swTicks
                : Convert.ToInt64(swTicks * TheStopWatchTicksToDateTimeTicksConversionFactor.Value);

        private static readonly bool TheSwIsHighPrec;
        private static readonly double? TheStopWatchTicksToDateTimeTicksConversionFactor; 
        private static readonly long TheDateTimeTicksPerSecond;
        private static readonly long TheStopWatchTicksPerSecond;
        private static readonly ThreadLocal<long> TheSDtDifferential = new ThreadLocal<long>(false); 
        private static readonly ThreadLocal<bool> TheSIsCalibrated = new ThreadLocal<bool>(()=>false,false);
        private static readonly ThreadLocal<DateTime> TheSLastCalibTime = new ThreadLocal<DateTime>(() => DateTime.MinValue,false);
        private static readonly TimeSpan TheMaxTimeBeforeRecalibration = TimeSpan.FromMinutes(15);
        private const int NumberOfCalibrationSamples = 3;

    }
}