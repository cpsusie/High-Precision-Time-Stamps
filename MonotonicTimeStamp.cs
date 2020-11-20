using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HpTimesStamps.BigMath;

namespace HpTimesStamps
{
    /// <summary>
    /// A monotonic time stamp is a time stamp retrieved from a monotonic clock.
    /// Monotonic clocks have the property that for any two tick queries at T0 and T1
    /// OCCURRING IN PROCESS the ticks returned from query at T1 will always be greater
    /// than or equal to the ticks retrieved at query T0 (where T1 happens after T0).
    ///
    /// Using DateTime.Now as a timestamp source is problematic because it is based on a system /
    /// wall clock.  System / wall clocks can change forwards or backwards and thus can be problematic
    /// when used with the expectation DateTime t2 - DateTime t1 will give the time elapsed between t2 and t1
    /// (when DateTime t1 was recorded at T0 and DateTime t2 recorded at T1 where T1 was later in time than T0).
    /// In fact, the difference could even be negative -- saying that the later time was earlier:
    ///     Daylights savings adjustments
    ///     User adjustments to system clock
    ///     System clock synchronization with a reference clock
    ///     leap seconds, etc
    ///
    /// These timestamps:
    ///     1- are obtained from a source with a finer resolution that most DateTimes
    ///     2- are obtained from a source unaffected by adjustments to the system clock,
    ///        DST, leap seconds, etc.
    /// Advantages:
    ///     1- individually represented simply as ticks from a monotonic clock 
    ///     2- a reference date time is correlated with a reference number of ticks from the monotonic clock 
    ///     3- can be considered a reference date time and an offset period.
    ///     4- when subtracting one from another WITHIN PROCESS, will yield a duration as accurate as the monotonic
    ///        clock supplying ticks
    ///     5- most useful when used to compare elapsed time between events 
    /// Disadvantages:
    ///     1- NEVER SERIALIZE OR SHARE OUTSIDE OF PROCESS
    ///     2- when interpreted as a date time, in the rare circumstances where the system clock changes, will be misleading
    ///     3- NEVER SERIALIZE OR SHARE OUTSIDE OF PROCESS -- useful for RUNNING program only.  You could convert to a date time
    ///        and serialize but may lose accuracy if the system clock and the monotonic clock are no longer in lockstep.
    /// </summary>
    /// <typeparam name="TStampContext">The common context used by all stamps parameterized with this type</typeparam>
    /// <remarks>DO NOT USE ACROSS PROCESSES!  DO NOT SERIALIZE!  NOT FOR ARCHIVE!</remarks>
    public readonly struct MonotonicTimeStamp<TStampContext> : IEquatable<MonotonicTimeStamp<TStampContext>>, IComparable<MonotonicTimeStamp<TStampContext>> where TStampContext : unmanaged, IEquatable<TStampContext>, IComparable<TStampContext>, IMonotonicStampContext
    {
        /// <summary>
        /// Create a monotonic timestamp from a tick count retrieved from the reference clock.
        /// </summary>
        /// <param name="referenceTicks">the tick count</param>
        /// <returns>a monotonic timestamp</returns>
        public static MonotonicTimeStamp<TStampContext> CreateFromRefTicks(long referenceTicks) => new MonotonicTimeStamp<TStampContext>(referenceTicks);
       
        /// <summary>
        /// The stamp context shared by all timestamps parameterized by <typeparamref name="TStampContext"/>.
        /// </summary>
        public ref readonly TStampContext Context
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MonotonicTimeStampUtil<TStampContext>.StampContext;
        }

        /// <summary>
        /// Retrieve the data in the most honest/least problematic (though perhaps a little inconvenient)
        /// format available:
        ///     1- The UTC reference date time
        ///     2- the amount of time that elapsed between the utc reference time
        ///        and the recording of the stamp
        ///     3- the difference between the local reference time and utc reference time
        /// </summary>
        public (DateTime UtcReferenceTime, Duration OffsetFromReference, TimeSpan LocalUtcOffset) Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get =>
                (UtcReference,
                    (StopwatchTicksAsDuration - ReferenceTicksAsDuration), UtcLocalOffsetPeriod);
        }

        /// <summary>
        /// Amount of time elapsed since the Utc Reference Time between the timestamp being recorded and the utc reference time 
        /// </summary>
        public Duration ElapsedSinceUtcReference
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (StopwatchTicksAsDuration - ReferenceTicksAsDuration);
        }

        private Duration StopwatchTicksAsDuration
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Duration.FromStopwatchTicks(_stopWatchTicks);
        }

        static MonotonicTimeStamp()
        {
            ref readonly TStampContext context = ref MonotonicTimeStampUtil<TStampContext>.StampContext;
            UtcReference = context.UtcDateTimeBeginReference;
            long referenceTicks = context.ReferenceTicks;
            long swTicksPerSecond = context.TicksPerSecond; //e.g. 1000
            long tsTicksPerSecond = TimeSpan.TicksPerSecond; //e.g. 100
            Debug.Assert(swTicksPerSecond > 0 && tsTicksPerSecond > 0);
            long gcd = (long) Gcd((ulong) swTicksPerSecond, (ulong) tsTicksPerSecond);
            Debug.Assert(swTicksPerSecond % gcd == 0 && tsTicksPerSecond % gcd == 0);
            TheToTsTickConversionFactorNumerator = tsTicksPerSecond / gcd;
            ToToTsTickConversionFactorDenominator = swTicksPerSecond / gcd;
            UtcLocalOffsetPeriod = context.UtcLocalTimeOffset;
            ReferenceTicksAsDuration = Duration.FromStopwatchTicks(referenceTicks);
        }

        /// <summary>
        /// Private CTOR
        /// </summary>
        /// <param name="stopwatchTicks"></param>
        private MonotonicTimeStamp(long stopwatchTicks) => _stopWatchTicks = stopwatchTicks;
        
        /// <summary>
        /// Convert the timestamp to a local date time.  If the system clock and the reference
        /// monotonic clock are out of sync (drift or adjustments made to system clock or time -- user edit,
        /// syncing of system clock, daylight savings adjustments, leap seconds, etc), may not be accurate.
        /// </summary>
        /// <returns>a datetime</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ToLocalDateTime() => DateTime.SpecifyKind( (UtcReference + ((TimeSpan) ElapsedSinceUtcReference)) + UtcLocalOffsetPeriod, DateTimeKind.Local);
        /// <summary>
        /// Convert the timestamp to a utc date time.  If the system clock and the reference
        /// monotonic clock are out of sync (drift or adjustments made to system clock or time -- user edit,
        /// syncing of system clock, leap seconds, etc), may not be accurate.
        /// </summary>
        /// <returns>a date-time</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ToUtcDateTime() => DateTime.SpecifyKind(UtcReference + ((TimeSpan) ElapsedSinceUtcReference), DateTimeKind.Utc);
        
        /// <summary>
        /// Tests two stamps for value equality
        /// </summary>
        /// <param name="lhs">left hand comparand</param>
        /// <param name="rhs">right hand comparand</param>
        /// <returns>true if they have the same value, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(MonotonicTimeStamp<TStampContext> lhs, MonotonicTimeStamp<TStampContext> rhs) =>
            lhs._stopWatchTicks == rhs._stopWatchTicks;
        /// <summary>
        /// Tests two stamps for value inequality
        /// </summary>
        /// <param name="lhs">left hand comparand</param>
        /// <param name="rhs">right hand comparand</param>
        /// <returns>true if they have the distinct values, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(MonotonicTimeStamp<TStampContext> lhs, MonotonicTimeStamp<TStampContext> rhs) =>
            !(lhs == rhs);
        /// <summary>
        /// Test to see whether the left hand comparand is greater than (succeeds in sort order) the right hand comparand.
        /// </summary>
        /// <param name="lhs">The left hand comparand</param>
        /// <param name="rhs">the right hand comparand</param>
        /// <returns>true if the left hand comparand is greater than the right hand comparand, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(MonotonicTimeStamp<TStampContext> lhs, MonotonicTimeStamp<TStampContext> rhs) =>
            lhs._stopWatchTicks > rhs._stopWatchTicks;
        /// <summary>
        /// Test to see whether the left hand comparand is less than (precedes in sort order) the right hand comparand.
        /// </summary>
        /// <param name="lhs">The left hand comparand</param>
        /// <param name="rhs">the right hand comparand</param>
        /// <returns>true if the left hand comparand is less than the right hand comparand, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(MonotonicTimeStamp<TStampContext> lhs, MonotonicTimeStamp<TStampContext> rhs) =>
            lhs._stopWatchTicks < rhs._stopWatchTicks;
        /// <summary>
        /// Test to see whether the left hand comparand is greater than (i.e. succeeds in sort-order) or equal to (i.e. is identical
        /// with and therefore holds sample place in sort order as) the right hand comparand.
        /// </summary>
        /// <param name="lhs">The left hand comparand</param>
        /// <param name="rhs">the right hand comparand</param>
        /// <returns>true if the left hand comparand is greater than or equal to the right hand comparand, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(MonotonicTimeStamp<TStampContext> lhs, MonotonicTimeStamp<TStampContext> rhs) =>
            !(lhs < rhs);
        /// <summary>
        /// Test to see whether the left hand comparand is less than than (i.e. precedes in sort-order) or equal to (i.e. is identical
        /// with and therefore holds sample place in sort order as) the right hand comparand.
        /// </summary>
        /// <param name="lhs">The left hand comparand</param>
        /// <param name="rhs">the right hand comparand</param>
        /// <returns>true if the left hand comparand is less than or equal to the right hand comparand, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(MonotonicTimeStamp<TStampContext> lhs, MonotonicTimeStamp<TStampContext> rhs) =>
            !(lhs > rhs);
        /// <summary>
        /// Get a value-based hash code for this object
        /// </summary>
        /// <returns>a hash code</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _stopWatchTicks.GetHashCode();
        /// <summary>
        /// Test another object to see whether it has the same value as this one.
        /// </summary>
        /// <param name="other">The other object</param>
        /// <returns>True if the other object is of the same type as this one and has
        /// the same value as this one.  False otherwise.</returns>
        public override bool Equals(object other) => other is MonotonicTimeStamp<TStampContext> mts && mts == this;
        /// <summary>
        /// Test to see whether another value of this type has the same value as this one
        /// </summary>
        /// <param name="other">the other value</param>
        /// <returns>true if same value, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(MonotonicTimeStamp<TStampContext> other) => other == this;
        /// <summary>
        /// Compare this stamp with another to establish the ordering between them
        /// </summary>
        /// <param name="other">the other</param>
        /// <returns>A negative number if the this value precedes the other value in the sort order.
        /// Zero if the this value is identical to the other value (and thus occupies same space in sort order).
        /// A positive number if this value succeeds the other value in sort order.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(MonotonicTimeStamp<TStampContext> other) =>
            _stopWatchTicks.CompareTo(other._stopWatchTicks);

        /// <summary>
        /// Add a stamp and duration together yielding a stamp
        /// </summary>
        /// <param name="lhs">left hand stamp operand</param>
        /// <param name="rhs">right hand duration operand</param>
        /// <returns>a stamp that is the result of adding a stamp and duration together</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MonotonicTimeStamp<TStampContext> operator
            +(MonotonicTimeStamp<TStampContext> lhs, Duration rhs) =>
            new MonotonicTimeStamp<TStampContext>(lhs._stopWatchTicks + (long) rhs._ticks);
        
        /// <summary>
        /// Add a stamp and duration together yielding a stamp
        /// </summary>
        /// <param name="lhs">left hand duration operand</param>
        /// <param name="rhs">right hand stamp operand</param>
        /// <returns>a stamp that is the result of adding a stamp and duration together</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MonotonicTimeStamp<TStampContext> operator
            +(Duration lhs, MonotonicTimeStamp<TStampContext> rhs) =>
            new MonotonicTimeStamp<TStampContext>(rhs._stopWatchTicks + (long) lhs._ticks);
        /// <summary>
        /// Subtract the right hand stamp from the left hand comparand, yielding
        /// the time elapsed between the two.
        /// </summary>
        /// <param name="lhs">the left hand operand</param>
        /// <param name="rhs">the right and operand</param>
        /// <returns>the amount of time elapsed between the operands</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Duration operator
            -(MonotonicTimeStamp<TStampContext> lhs, MonotonicTimeStamp<TStampContext> rhs) =>
            Duration.FromStopwatchTicks(lhs._stopWatchTicks - rhs._stopWatchTicks);
        /// <summary>
        /// Subtract a duration from a stamp yielding a stamp
        /// </summary>
        /// <param name="lhs">the stamp minuend</param>
        /// <param name="rhs">the duration subtrahend</param>
        /// <returns>A stamp difference</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MonotonicTimeStamp<TStampContext> operator
            -(MonotonicTimeStamp<TStampContext> lhs, Duration rhs) =>
            new MonotonicTimeStamp<TStampContext>(lhs._stopWatchTicks - (long) rhs._ticks);
        /// <summary>
        /// Get a string representation of this value
        /// </summary>
        /// <returns>a string representation.</returns>
        /// <remarks>Will be printed as a local DateTime represented in ISO format.  If
        /// there has been significant drift between monotonic clock and system clock
        /// or if the system clock has changed due to user adjustments, synchro with clock server,
        /// daylight savings time, leap seconds ... may be inaccurate or misleading.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => ToLocalDateTime().ToString("O");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ConvertStopwatchTicksToReferenceTicks(long stopwatchTicks)
        {
#if DEBUG
            checked
            {
                return (stopwatchTicks * TheToTsTickConversionFactorNumerator) / ToToTsTickConversionFactorDenominator;
            }
#else
            return (stopwatchTicks * TheToTsTickConversionFactorNumerator) / ToToTsTickConversionFactorDenominator;
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ConvertTsTicksToSwTicks(long ticks)
        {
#if DEBUG
            checked
            {
                return (ticks * ToToTsTickConversionFactorDenominator) / TheToTsTickConversionFactorNumerator;
            }
#else
            return (ticks * ToToTsTickConversionFactorDenominator) / TheToTsTickConversionFactorNumerator;
#endif
        }

        private static ulong Gcd(ulong a, ulong b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }
            return a | b;
        }

        // ReSharper disable StaticMemberInGenericType -- intentional and necessary
        private static readonly Duration ReferenceTicksAsDuration; 
        private static readonly DateTime UtcReference;
        private static readonly TimeSpan UtcLocalOffsetPeriod;
        internal static readonly long TheToTsTickConversionFactorNumerator;
        internal static readonly long ToToTsTickConversionFactorDenominator;
        // ReSharper restore StaticMemberInGenericType
        private readonly long _stopWatchTicks;
    }

    internal static class TimeSpanExtensions
    {
        /// <summary>
        /// Convert a timespan into whole nanoseconds (discarding any fractional part left over)
        /// </summary>
        /// <param name="t">The timespan</param>
        /// <returns>A 128-bit integer representing the number of whole nanoseconds (discarding leftover)
        /// represented by <paramref name="t"/>.</returns>
        internal static Int128 TotalWholeNanoseconds(this TimeSpan t)
        {
            Int128 ticksPerSecond = TimeSpan.TicksPerSecond;
            return ticksPerSecond == PortableMonotonicStamp.NanosecondsFrequency
                ? t.Ticks
                : ((Int128) t.Ticks) * (PortableMonotonicStamp.NanosecondsFrequency / ticksPerSecond);
        }

        

        /// <summary>
        /// Convert foreign nanoseconds to a local duration
        /// </summary>
        /// <param name="convertMe">the foreign nanoseconds to convert</param>
        /// <returns>the local duration.</returns>
        internal static Duration ConvertForeignNanosecondsToLocalDuration(this in Int128 convertMe) =>
            Stopwatch.Frequency == PortableMonotonicStamp.NanosecondsFrequency
                ? Duration.FromStopwatchTicks(in convertMe)
                : Duration.FromStopwatchTicks(convertMe *
                                              (PortableMonotonicStamp.NanosecondsFrequency / Stopwatch.Frequency));
    }
}