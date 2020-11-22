using System;

namespace HpTimeStamps
{
    /// <summary>
    /// If the runtime / hardware does not have a monotonic clock available,  this exception is thrown.
    /// </summary>
    public sealed class MonotonicClockNotAvailableException : ApplicationException
    {
        /// <summary>
        /// CTOR
        /// </summary>
        public MonotonicClockNotAvailableException() : base(MessageText) { }

        private const string MessageText = "No monotonic clock support is available on this platform.";
    }
}