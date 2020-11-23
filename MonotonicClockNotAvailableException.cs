using System;

namespace HpTimeStamps
{
    /// <summary>
    /// If the runtime / hardware does not have a monotonic clock available,  this exception is thrown.
    /// </summary>
    public sealed class MonotonicClockNotAvailableException : UnsupportedStopwatchException
    {
        /// <summary>
        /// CTOR
        /// </summary>
        public MonotonicClockNotAvailableException() : base(MessageText, null) { }

        private const string MessageText = "No monotonic clock support is available on this platform.";
    }
}