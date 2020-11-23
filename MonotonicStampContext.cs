using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using HpTimeStamps.BigMath;
using JetBrains.Annotations;

namespace HpTimeStamps
{
    /// <summary>
    /// The implementation of <see cref="IMonotonicStampContext"/> this library provides.
    /// </summary>
    [DataContract]
    public readonly struct MonotonicStampContext : IMonotonicStampContext, IEquatable<MonotonicStampContext>, IComparable<MonotonicStampContext>
    {
        /// <summary>
        /// Create a stamp context to serve as reference point for monotonic timestamps
        /// </summary>
        /// <returns>context value</returns>
        public static MonotonicStampContext CreateStampContext()
        {
            Guid id = Guid.NewGuid();
            long refTicks = Stopwatch.GetTimestamp();
            DateTime localTime = DateTime.Now;
            DateTime utcTime = localTime.ToUniversalTime();
            long ticksPerSecond = Stopwatch.Frequency;
            return new MonotonicStampContext(in id, utcTime, localTime, refTicks, ticksPerSecond);
        }

        #region Properties

        /// <summary>
        /// # nano seconds in a second
        /// </summary>
        public const long NanosecondsPerSecond = 1_000_000_000;
        
        /// <summary>
        /// number of nanoseconds in a second
        /// </summary>
        public long NanosecondsFrequency => NanosecondsPerSecond;

        /// <inheritdoc />
        public bool EasyConversionAllWays => EasyConversionToAndFromNanoseconds && EasyConversionToAndFromTimespanTicks;

        /// <inheritdoc />
        public bool EasyConversionToAndFromTimespanTicks { get; }

        /// <inheritdoc />
        public bool EasyConversionToAndFromNanoseconds { get; }

        /// <inheritdoc />
        public bool IsInvalid => ContextId == default;
        /// <inheritdoc />
        [DataMember] public Guid ContextId { get; }
        /// <inheritdoc />
        [DataMember] public DateTime UtcDateTimeBeginReference { get; }
        /// <inheritdoc />
        public DateTime LocalTimeBeginReference =>
            DateTime.SpecifyKind(UtcDateTimeBeginReference + UtcLocalTimeOffset, DateTimeKind.Local);
        /// <inheritdoc />
        [DataMember] public long ReferenceTicks { get; }
        /// <inheritdoc />
        [DataMember] public long TicksPerSecond { get; }
        /// <inheritdoc />
        [DataMember] public TimeSpan UtcLocalTimeOffset { get; }
        
        /// <summary>
        /// The utc local offset expressed as a duration
        /// </summary>
        public ref readonly Duration UtcLocalTimeOffsetAsDuration
        {
            get
            {
                ref readonly Duration ret = ref Duration.Zero;
                switch (_utcOffsetAsDuration?.IsSet)
                {
                    case true:
                        ret = ref _utcOffsetAsDuration.Value;
                        break;
                    case null:
                        break;
                    case false:
                        CalculateValue();
                        Debug.Assert(_utcOffsetAsDuration.IsSet, "Calculate value should guarantee it is set.");
                        ret = ref _utcOffsetAsDuration.Value;
                        break;
                }
                return ref ret;
            }
        }

        private void CalculateValue()
        {
            Debug.Assert(_utcOffsetAsDuration != null, "Do not call if null.");
            _utcOffsetAsDuration.TrySetValue(Duration.FromStopwatchTicks(((Int128) UtcLocalTimeOffset.Ticks) *
                TicksPerSecond / TimeSpan.TicksPerSecond));
            Debug.Assert(_utcOffsetAsDuration.IsSet,
                "Should always be set after call if no except ... whether set occured on this " +
                "thread with the preceding line or on a prior call on another thread.");
        }

        /// <inheritdoc />
        public bool AllTimestampsUtc => UtcDateTimeBeginReference == LocalTimeBeginReference; 
        #endregion

        #region Private CTOR
        private MonotonicStampContext(in Guid contextId, DateTime utcBeginRef, DateTime localTimeRef, long refTicks,
           long ticksPerSecond)
        {
            if (utcBeginRef.Kind != DateTimeKind.Utc) throw new ArgumentException(@"Parameter is not a utc datetime.", nameof(utcBeginRef));
            if (localTimeRef.Kind != DateTimeKind.Local) throw new ArgumentException(@"Parameter is not a local date time.", nameof(localTimeRef));
            if (localTimeRef.ToUniversalTime() != utcBeginRef) throw new ArgumentException($"Parameter {localTimeRef} is not the local variant of parameter {utcBeginRef}.");
            ContextId = contextId;
            ReferenceTicks = refTicks;
            UtcDateTimeBeginReference = utcBeginRef;
            UtcLocalTimeOffset = localTimeRef - UtcDateTimeBeginReference;
            TicksPerSecond = ticksPerSecond;
            EasyConversionToAndFromTimespanTicks = DetermineIsEasyConversion(TicksPerSecond, TimeSpan.TicksPerSecond);
            EasyConversionToAndFromNanoseconds = DetermineIsEasyConversion(TicksPerSecond, NanosecondsPerSecond);
            _utcOffsetAsDuration = new LocklessWriteOnceValue<Duration>();
            static bool DetermineIsEasyConversion(long frequencyOne, long frequencyTwo)
            {
                if (frequencyOne == frequencyTwo) return true;
                long bigger = Math.Max(frequencyOne, frequencyTwo);
                long smaller = Math.Min(frequencyOne, frequencyTwo);
                long tsConvertRem = bigger % smaller;
                long tsConvertQuot = bigger / smaller;
                return tsConvertRem == 0 && (tsConvertQuot == 1 || tsConvertQuot % 10 == 0);
            }
        } 
        #endregion


        /// <inheritdoc />
        public override string ToString() =>
            $"[{nameof(MonotonicStampContext)}] -- Reference Ticks: [{ReferenceTicks:N0}], " +
            $"Ticks / second: [{TicksPerSecond:N0}], UtcRef: [{UtcDateTimeBeginReference:O}], " +
            $"Local Offset: [{UtcLocalTimeOffset.TotalHours:N6}]";
        /// <summary>
        /// Get a value-based hash code for this object
        /// </summary>
        /// <returns>a hash code</returns>
        public override int GetHashCode() => ContextId.GetHashCode();
        /// <summary>
        /// Test another object to see whether it has the same value as this one.
        /// </summary>
        /// <param name="obj">The other object</param>
        /// <returns>True if the other object is of the same type as this one and has
        /// the same value as this one.  False otherwise.</returns>
        public override bool Equals(object obj) => obj is MonotonicStampContext msc && msc == this;
        /// <summary>
        /// Test to see whether another value of this type has the same value as this one
        /// </summary>
        /// <param name="other">the other value</param>
        /// <returns>true if same value, false otherwise</returns>
        public bool Equals(MonotonicStampContext other) => other == this;

        /// <summary>
        /// Tests two stamp contexts for value equality
        /// </summary>
        /// <param name="lhs">left hand comparand</param>
        /// <param name="rhs">right hand comparand</param>
        /// <returns>true if they have the same value, false otherwise</returns>
        public static bool operator ==(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            lhs.ContextId == rhs.ContextId && lhs.UtcDateTimeBeginReference == rhs.UtcDateTimeBeginReference &&
            lhs.ReferenceTicks == rhs.ReferenceTicks && lhs.TicksPerSecond == rhs.TicksPerSecond &&
            lhs.UtcLocalTimeOffset == rhs.UtcLocalTimeOffset;
        /// <summary>
        /// Tests two stamp contexts for value inequality
        /// </summary>
        /// <param name="lhs">left hand comparand</param>
        /// <param name="rhs">right hand comparand</param>
        /// <returns>true if they have the distinct values, false otherwise</returns>
        public static bool operator !=(in MonotonicStampContext lhs, in MonotonicStampContext rhs) => !(lhs == rhs);
        /// <summary>
        /// Test to see whether the left hand comparand is greater than (succeeds in sort order) the right hand comparand.
        /// </summary>
        /// <param name="lhs">The left hand comparand</param>
        /// <param name="rhs">the right hand comparand</param>
        /// <returns>true if the left hand comparand is greater than the right hand comparand, false otherwise</returns>
        public static bool operator >(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            Compare(in lhs, in rhs) > 0;
        /// <summary>
        /// Test to see whether the left hand comparand is less than (precedes in sort order) the right hand comparand.
        /// </summary>
        /// <param name="lhs">The left hand comparand</param>
        /// <param name="rhs">the right hand comparand</param>
        /// <returns>true if the left hand comparand is less than the right hand comparand, false otherwise</returns>
        public static bool operator <(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            Compare(in lhs, in rhs) < 0;
        /// <summary>
        /// Test to see whether the left hand comparand is greater than (i.e. succeeds in sort-order) or equal to (i.e. is identical
        /// with and therefore holds sample place in sort order as) the right hand comparand.
        /// </summary>
        /// <param name="lhs">The left hand comparand</param>
        /// <param name="rhs">the right hand comparand</param>
        /// <returns>true if the left hand comparand is greater than or equal to the right hand comparand, false otherwise</returns>
        public static bool operator >=(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            !(lhs < rhs);
        /// <summary>
        /// Test to see whether the left hand comparand is less than than (i.e. precedes in sort-order) or equal to (i.e. is identical
        /// with and therefore holds sample place in sort order as) the right hand comparand.
        /// </summary>
        /// <param name="lhs">The left hand comparand</param>
        /// <param name="rhs">the right hand comparand</param>
        /// <returns>true if the left hand comparand is less than or equal to the right hand comparand, false otherwise</returns>
        public static bool operator <=(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            !(lhs > rhs);

        
        
       
        
        /// <summary>
        /// Compare this context with another to establish the ordering between them
        /// </summary>
        /// <param name="other">the other</param>
        /// <returns>A negative number if the this value precedes the other value in the sort order.
        /// Zero if the this value is identical to the other value (and thus occupies same space in sort order).
        /// A positive number if this value succeeds the other value in sort order.</returns>
        public int CompareTo(MonotonicStampContext other) => Compare(in this, in other);

        static MonotonicStampContext()
        {
            if (!Stopwatch.IsHighResolution)
            {
                throw new MonotonicClockNotAvailableException();
            }
        }

        /// <summary>
        /// Compare two monotonic stamp contexts to establish their
        /// positions relative to each other in the sort order.
        /// </summary>
        /// <param name="lhs">left-hand comparand</param>
        /// <param name="rhs">Right-hand comparand</param>
        /// <returns>A negative number if the left-hand comparand precedes the right-hand comparand in the sort order.
        /// Zero if the left-hand comparand is identical to the right hand comparand (and thus occupies same space in sort order).
        /// A positive number if the left-hand comparand succeeds the right-hand comparand in sort order.</returns>
        public static int Compare(in MonotonicStampContext lhs, in MonotonicStampContext rhs)
        {
            int ret;
            int idComp = lhs.ContextId.CompareTo(rhs.ContextId);
            if (idComp == 0)
            {
                int utcComp = lhs.UtcDateTimeBeginReference.CompareTo(rhs.UtcDateTimeBeginReference);
                if (utcComp == 0)
                {
                    int refTickCount = lhs.ReferenceTicks.CompareTo(rhs.ReferenceTicks);
                    if (refTickCount == 0)
                    {
                        int tpsComp = lhs.TicksPerSecond.CompareTo(rhs.TicksPerSecond);
                        ret = tpsComp == 0 ? lhs.UtcLocalTimeOffset.CompareTo(rhs.UtcLocalTimeOffset) : tpsComp;
                    }
                    else
                    {
                        ret = refTickCount;
                    }
                }
                else
                {
                    ret = utcComp;
                }
            }
            else
            {
                ret = idComp;
            }
            return ret;
        }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        private readonly LocklessWriteOnceValue<Duration> _utcOffsetAsDuration;
    }
}