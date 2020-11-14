using System;

namespace HpTimesStamps
{
    /// <summary>
    /// The monotonic time stamp context should be implemented
    /// as a readonly struct
    /// </summary>
    public interface IMonotonicStampContext
    {
        /// <summary>
        /// True if the struct implementing this interface
        /// was not properly initialized.  Usually
        /// testing <see cref="ContextId"/> against
        /// the <see langword = "default"/> value of <see cref="Guid"/>
        /// is sufficient
        /// </summary>
        bool IsInvalid { get; }
        /// <summary>
        /// Unique id
        /// </summary>
        Guid ContextId { get; }
        /// <summary>
        /// True if <see cref="UtcDateTimeBeginReference"/> equals
        /// <see cref="LocalTimeBeginReference"/>, false otherwise
        /// </summary>
        bool AllTimestampsUtc { get; }
        /// <summary>
        /// The UTC neutral date time represented by
        /// <see cref="ReferenceTicks"/>.
        /// </summary>
        DateTime UtcDateTimeBeginReference { get; }
        /// <summary>
        /// The local time version of <see cref="UtcDateTimeBeginReference"/>.
        /// If <see cref="AllTimestampsUtc"/>, this value is equal to <see cref="UtcDateTimeBeginReference"/>.
        /// </summary>
        DateTime LocalTimeBeginReference { get; }
        /// <summary>
        /// The number of clock ticks that equals <see cref="UtcDateTimeBeginReference"/>
        /// </summary>
        long ReferenceTicks { get; }
        /// <summary>
        /// The number of clock ticks in one second
        /// </summary>
        long TicksPerSecond { get; }
        /// <summary>
        /// Difference between <see cref="UtcDateTimeBeginReference"/> and <see cref="LocalTimeBeginReference"/>.
        /// </summary>
        TimeSpan UtcLocalTimeOffset { get; }
    }

    
}