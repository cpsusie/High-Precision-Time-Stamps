using System;

namespace ExampleTimestamps
{
    /// <summary>
    /// Implementations hereof used by <see cref="TimeStampProvider"/> to provide default
    /// DateTime format timestamps;
    /// </summary>
    public abstract class DefaultStampProvider
    {
        /// <summary>
        /// The source the <see cref="TimeStampProvider"/> uses to produce its
        /// default timestamps via the <see cref="TimeStampProvider.Now"/> and
        /// <see cref="TimeStampProvider.UtcNow"/>
        /// </summary>
        public abstract DefaultStampType DefaultStamp { get; }

        /// <summary>
        /// Get a timestamp expressing the current local time
        /// </summary>
        public abstract DateTime DefaultNow { get; }
        /// <summary>
        /// Get a timestamp expressing the current utc time
        /// </summary>
        public abstract DateTime DefaultUtcNow { get; }
    }
}