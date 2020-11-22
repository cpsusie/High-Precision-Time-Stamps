using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HpTimeStamps.BigMath;

namespace HpTimeStamps
{
    /// <summary>
    /// A portable monotonic stamp suitable for use across process boundaries and
    /// for serialization.  Time is stored in UTC so any information about local time zone will
    /// be lost.
    ///
    ///  Represented internally as a 128 bit signed integer that represents nanoseconds elapsed
    ///  since <see cref="DateTime.MinValue"/>, which is January 1, 0001 AD (UTC).
    /// </summary>
    [DataContract]
    public readonly struct PortableMonotonicStamp : IEquatable<PortableMonotonicStamp>,
        IComparable<PortableMonotonicStamp>
    {
        /// <summary>
        /// Number of nanoseconds in a second
        /// </summary>
        public const long NanosecondsFrequency = 1_000_000_000;

        internal PortableMonotonicStamp(in Int128 nanosecondSinceDtUtcEpoch) =>
            _dateTimeNanosecondOffsetFromMinValueUtc = nanosecondSinceDtUtcEpoch;

        /// <summary>
        /// Convert this to a datetime in the local datetime timezone.
        /// </summary>
        /// <returns>A local datetime</returns>
        /// <exception cref="PortableTimestampOverflowException"></exception>
        /// <exception cref="PortableTimestampOverflowException">Cannot be converted to
        /// DateTime because of overflow.</exception>
        [System.Diagnostics.Contracts.Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ToLocalDateTime()
        {
            try
            {
                DateTime utc = ToUtcDateTime();
                return utc.ToLocalTime();
            }
            catch (ArgumentOutOfRangeException inner)
            {
                throw new PortableTimestampOverflowException(
                    "Overflow prevented conversion of portable monotonic stamp to a local DateTime.", inner);
            }
        }

        /// <summary>
        /// Convert this into a utc datetime
        /// </summary>
        /// <returns>a utc datetime</returns>
        /// <exception cref="PortableTimestampOverflowException">Cannot be converted to
        /// DateTime because of overflow.</exception>
        [System.Diagnostics.Contracts.Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ToUtcDateTime()
        {
            try
            {
                TimeSpan dtFromMin = CreateTimespanFromNanoseconds(_dateTimeNanosecondOffsetFromMinValueUtc);
                DateTime utcTime = DateTime.SpecifyKind(DateTime.MinValue + dtFromMin, DateTimeKind.Utc);
                return utcTime;
            }
            catch (ArgumentOutOfRangeException inner)
            {
                throw new PortableTimestampOverflowException("Overflow prevented conversion of portable monotonic stamp to a UTC DateTime.", inner);
            }
        }

        /// <summary>
        /// Get a string representation of this value
        /// </summary>
        /// <returns>A string representation.</returns>
        public override string ToString() =>
            $".NET Datetime epoch + {_dateTimeNanosecondOffsetFromMinValueUtc} nanoseconds";
        

        /// <summary>
        /// Test two portable monotonic stamps for value equality
        /// </summary>
        /// <param name="lhs">left hand operand</param>
        /// <param name="rhs">right hand operand</param>
        /// <returns>true if they have the same value, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) =>
            lhs._dateTimeNanosecondOffsetFromMinValueUtc == rhs._dateTimeNanosecondOffsetFromMinValueUtc;
        /// <summary>
        /// Test two portable monotonic stamps for value inequality
        /// </summary>
        /// <param name="lhs">left hand operand</param>
        /// <param name="rhs">right hand operand</param>
        /// <returns>true if they have different values, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) =>
            !(lhs == rhs);
        /// <summary>
        /// Test two portable monotonic stamps to see if <paramref name="lhs"/> is greater than
        /// <paramref name="rhs"/>.
        /// </summary>
        /// <param name="lhs">left hand operand</param>
        /// <param name="rhs">right hand operand</param>
        /// <returns>true if <paramref name="lhs"/> is greater than
        /// <paramref name="rhs"/>, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) =>
            lhs._dateTimeNanosecondOffsetFromMinValueUtc > rhs._dateTimeNanosecondOffsetFromMinValueUtc;

        /// <summary>
        /// Test two portable monotonic stamps to see if <paramref name="lhs"/> is less than
        /// <paramref name="rhs"/>.
        /// </summary>
        /// <param name="lhs">left hand operand</param>
        /// <param name="rhs">right hand operand</param>
        /// <returns>true if <paramref name="lhs"/> is less than
        /// <paramref name="rhs"/>, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) =>
            lhs._dateTimeNanosecondOffsetFromMinValueUtc < rhs._dateTimeNanosecondOffsetFromMinValueUtc;

        /// <summary>
        /// Test two portable monotonic stamps to see if <paramref name="lhs"/> is greater or equal to than
        /// <paramref name="rhs"/>.
        /// </summary>
        /// <param name="lhs">left hand operand</param>
        /// <param name="rhs">right hand operand</param>
        /// <returns>true if <paramref name="lhs"/> is greater than
        /// <paramref name="rhs"/>, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) =>
            !(lhs < rhs);

        /// <summary>
        /// Test two portable monotonic stamps to see if <paramref name="lhs"/> is less or equal to than
        /// <paramref name="rhs"/>.
        /// </summary>
        /// <param name="lhs">left hand operand</param>
        /// <param name="rhs">right hand operand</param>
        /// <returns>true if <paramref name="lhs"/> is less than or equal to
        /// <paramref name="rhs"/>, false otherwise</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs) =>
            !(lhs > rhs);

        /// <summary>
        /// A value based hash code for this value
        /// </summary>
        /// <returns>a hash code</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _dateTimeNanosecondOffsetFromMinValueUtc.GetHashCode();

        /// <summary>
        /// Check to see if this value has the same value as another object
        /// </summary>
        /// <param name="obj">the other object</param>
        /// <returns>True if the other object is a <see cref="PortableMonotonicStamp"/>
        /// that has the same value as this one.  False otherwise
        /// </returns>
        public override bool Equals(object obj) => obj is PortableMonotonicStamp pmts && pmts == this;
        /// <summary>
        /// Test to see whether this value is the same as another value
        /// of the same type.
        /// </summary>
        /// <param name="other">the other value</param>
        /// <returns>True if the other value has the same value as this one, false otherwise.</returns>
        public bool Equals(PortableMonotonicStamp other) => other == this;
        
        /// <summary>
        /// Compare this value to another value of the same type to establish the ordering relation between them.
        /// </summary>
        /// <param name="other">the other object</param>
        /// <returns>
        /// Zero if this value and <paramref name="other"/> have identical position within sort order.
        /// A positive number if this value succeeds <paramref name="other"/> in the sort order.
        /// A negative number if this value precedes <paramref name="other"/> in the sort order.
        /// </returns>
        public int CompareTo(PortableMonotonicStamp other) => Compare(in this, in other);
        
        
        /// <summary>
        /// Compare two <see cref="PortableMonotonicStamp"/> values to establish the ordering between them.
        /// </summary>
        /// <param name="lhs">Left hand operand</param>
        /// <param name="rhs">Right hand operand</param>
        /// <returns>
        /// Zero if <paramref name="lhs"/> and <paramref name="rhs"/> have identical position within sort order.
        /// A positive number if <paramref name="lhs"/> succeeds <paramref name="rhs"/> in the sort order.
        /// A negative number if <paramref name="lhs"/> precedes <paramref name="rhs"/> in the sort order.
        /// </returns>
        public static int Compare(in PortableMonotonicStamp lhs, in PortableMonotonicStamp rhs)
        {
            if (lhs == rhs) return 0;
            return lhs > rhs ? 1 : -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Duration CreateLocalDurationFromNanoseconds(in Int128 nanoseconds)
        {
            //Assume that no system has resolution finer than nanoseconds
            Int128 localTicks = nanoseconds * (NanosecondsFrequency / LocalStopwatchFrequency);
            return Duration.FromStopwatchTicks(in localTicks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Int128 CreateNanosecondsFromLocalDuration(in Duration duration) =>
            duration._ticks * (LocalStopwatchFrequency / NanosecondsFrequency);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DateTime CreateDateTimeFromNanoseconds(in Int128 nanoseconds)
        {
            TimeSpan ts = CreateTimespanFromNanoseconds(nanoseconds);
            return DateTime.MinValue + ts;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TimeSpan CreateTimespanFromNanoseconds(Int128 nanoseconds)
        {
            nanoseconds = nanoseconds * (TimeSpan.TicksPerSecond / NanosecondsFrequency);
            if (nanoseconds > TimeSpan.MaxValue.Ticks)
            {
                throw new ArgumentOutOfRangeException(nameof(nanoseconds), nanoseconds,
                    "Value does not fit in a timespan.");
            }
            return TimeSpan.FromTicks((long)nanoseconds);
        }

        private static Int128 StopwatchTicksFromNanoseconds(in Int128 nanoseconds) => nanoseconds * (LocalStopwatchFrequency / NanosecondsFrequency);

        private static readonly long LocalStopwatchFrequency = Stopwatch.Frequency;
        // ReSharper disable once InconsistentNaming -- internal where private normal for efficiency
        [DataMember] internal readonly Int128 _dateTimeNanosecondOffsetFromMinValueUtc;
        
    }
}