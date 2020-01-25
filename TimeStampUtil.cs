using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace HpTimesStamps
{
    [SuppressMessage("ReSharper", "LocalizableElement")]
    public readonly struct TimeStampUtil : ITimeStampUtil, IEquatable<TimeStampUtil>, IEquatable<ITimeStampUtil>
    {
        public bool IsHighPrecision => TheSwIsHighPrec;
        public bool IsCalibrated => TheSIsCalibrated.Value && TimeSinceLastCalibration < TheMaxTimeBeforeRecalibration;
        public long StopWatchTicksPerSecond => TheStopWatchTicksPerSecond;
        public long DateTimeTicksPerSecond => TheDateTimeTicksPerSecond;
        public TimeSpan TimeSinceLastCalibration => DateTime.Now - TheSLastCalibTime.Value;
        public TimeSpan CalibrationWindow => TheMaxTimeBeforeRecalibration;
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

        public void Calibrate() => DoCalibrate();

        public static bool operator ==(TimeStampUtil lhs, TimeStampUtil rhs) => true;
        public static bool operator !=(TimeStampUtil lhs, TimeStampUtil rhs) => !(lhs == rhs);
        public bool Equals(TimeStampUtil other) => true;
        public override int GetHashCode() => 397;
        public bool Equals(ITimeStampUtil other) => other as TimeStampUtil? == this;
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