using System;
using JetBrains.Annotations;

namespace HpTimeStamps
{
    /// <summary>
    /// An exception that is thrown to indicate that the system's stopwatch does not meet minimum resolution requirements
    /// for the stopwatch.
    /// </summary>
    public sealed class UnsupportedStopwatchResolutionException : UnsupportedStopwatchException
    {
        /// <summary>
        /// The frequency of the current system's stopwatch
        /// </summary>
        public long ActualStopwatchFrequency { get; }
        /// <summary>
        /// The minimum supported frequency for this system's stopwatch
        /// </summary>
        public long RequiredStopwatchMinimumFrequency { get; }

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="actualFrequency">actual frequency of this system's stopwatch</param>
        /// <param name="minimumRequiredFrequency">the minimum frequency this library supports</param>
        public UnsupportedStopwatchResolutionException(long actualFrequency, long minimumRequiredFrequency) : this(actualFrequency, minimumRequiredFrequency, string.Empty, null) {}

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="actualFrequency">actual frequency of this system's stopwatch</param>
        /// <param name="minimumRequiredFrequency">the minimum frequency this library supports</param>
        /// <param name="extraInfo">optionally, extra information</param>
        /// <param name="inner">optionally, an inner exception that caused this exception.</param>
        public UnsupportedStopwatchResolutionException(long actualFrequency, long minimumRequiredFrequency,
            [CanBeNull] string extraInfo, [CanBeNull] Exception inner) : base(
            CreateMessage(actualFrequency, minimumRequiredFrequency, extraInfo, inner), inner)
        {
            ActualStopwatchFrequency = actualFrequency;
            RequiredStopwatchMinimumFrequency = minimumRequiredFrequency;
        }

        private static string CreateMessage(long actual, long expectedMinimum, [CanBeNull] string extraInfo,
            [CanBeNull] Exception inner)
        {
            string baseMsg =
                $"This libraries requires a stopwatch frequency of at least {expectedMinimum:N0} ticks per second but the current system has only a frequency of {actual:N0} ticks per second.";
            baseMsg += (!string.IsNullOrWhiteSpace(extraInfo)
                ? ("  Extra information: \"" + extraInfo + "\".")
                : string.Empty);
            baseMsg += (inner != null ? "  Consult inner exception for more details." : string.Empty);
            return baseMsg;
        }


    }
}