using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using HpTimeStamps.BigMath;

namespace HpTimeStamps
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
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")] //YES! Intended.  Every stamp context type should have its own static properties.
    public readonly struct MonotonicTimeStamp<TStampContext> : IEquatable<MonotonicTimeStamp<TStampContext>>, IComparable<MonotonicTimeStamp<TStampContext>> where TStampContext : struct, IEquatable<TStampContext>, IComparable<TStampContext>, IMonotonicStampContext
    {
        /// <summary>
        /// The maximum <see cref="DateTime"/> capable of being expressed in terms of the current monotonic context (i.e. converted into 
        /// a monotonic stamp).  Importing date times into monotonic stamps is not recommended.  Instead,
        /// if the monotonic stamp is going to be meaningful for beyond a brief period in the currently running process,
        /// it should be converted into a <see cref="DateTime"/> or a <see cref="PortableMonotonicStamp"/>.
        /// </summary>
        /// <remarks>
        /// This amount will vary depending on system configuration.  Do not make assumptions -- it can get pretty strange on
        /// some systems.
        /// </remarks>
        public static DateTime MaximumImportableDateTime { get; }

        /// <summary>
        /// The minimum <see cref="DateTime"/> capable of being expressed in terms of the current monotonic context (i.e. converted into 
        /// a monotonic stamp).  Importing date times into monotonic stamps is not recommended.  Instead,
        /// if the monotonic stamp is going to be meaningful for beyond a brief period in the currently running process,
        /// it should be converted into a <see cref="DateTime"/> or a <see cref="PortableMonotonicStamp"/>.
        /// </summary>
        /// <remarks>
        /// This amount will vary depending on system configuration.  Do not make assumptions -- it can get pretty strange on
        /// some systems.
        /// </remarks>
        public static DateTime MinimumImportableDateTime { get; }

        /// <summary>
        /// The timestamp that represents the reference time exactly.  It's <see cref="Value"/> property
        /// should have it's "OffsetFromReference" property set to exactly zero.
        /// </summary>
        public static MonotonicTimeStamp<TStampContext> ReferenceTimeStamp { get; }
        /// <summary>
        /// The maximum value of the monotonic timestamp in the current process.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="DateTime"/> and <see cref="PortableMonotonicStamp"/>, this value may change from system to system.
        /// Make no assumptions about it beyond the promise it will not change in the currently running process.
        /// </remarks>
        public static MonotonicTimeStamp<TStampContext> MaxValue { get; }
        /// <summary>
        /// The minimum value of the monotonic timestamp in the current process.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="DateTime"/> and <see cref="PortableMonotonicStamp"/>, this value may change from system to system.
        /// Make no assumptions about it beyond the promise it will not change in the currently running process.
        /// </remarks>
        public static MonotonicTimeStamp<TStampContext> MinValue { get; }

        /// <summary>
        /// Convert a portable timestamp to a local timestamp
        /// </summary>
        /// <param name="ts">the portable timestamp to convert</param>
        /// <returns>The portable timestamp expressed in terms of the local in-process <typeparamref name="TStampContext"/>.</returns>
        /// <exception cref="Exception">The portable stamp cannot be expressed in terms of the local monotonic <typeparamref name="TStampContext"/>.
        /// This is likely because it refers to a date time outside the range of <see cref="DateTime"/> or is too far from the <typeparamref name="TStampContext"/>
        /// reference time to store the offset in a <see cref="Int64"/>.
        /// </exception>
        public static explicit operator MonotonicTimeStamp<TStampContext>(in PortableMonotonicStamp ts) =>
            ImportPortableTimestamp(in ts);
        
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

        internal static ref readonly TStampContext StatContext
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
            (MaximumImportableDateTime, MinimumImportableDateTime, MaxValue, MinValue) = CalculateMaximumAndMinimumImportableDateTimes();
            ReferenceTimeStamp = new MonotonicTimeStamp<TStampContext>(referenceTicks);
        }


        static (DateTime Max, DateTime Min, MonotonicTimeStamp<TStampContext> MaxStamp, MonotonicTimeStamp<TStampContext> MinStamp ) CalculateMaximumAndMinimumImportableDateTimes()
        {
            long dateTimeUtcReferenceTicks = UtcReference.Ticks;

            //bug issue 21 fix (7 day buffer -> 30 day buffer for .NET 5.0
            DateTime effectiveMinMonotonicDateTime = DateTime.MinValue.ToUniversalTime() + TimeSpan.FromDays(30);
            DateTime effectiveMaxMonotonicDateTime = DateTime.MaxValue.ToUniversalTime() - TimeSpan.FromDays(30);
            //Because MIN AND MAX are apparently UNSPECIFIED rather than UTC in some versions of .NET, it can screw up min max calculations.  Since we aren't likely to ever being collecting
            //monotonic stamps anywhere around min or max value, put a week buffer time in there to prevent conflict.
            long dateTimeMaxTimeSpanTicks = effectiveMaxMonotonicDateTime.Ticks;
            long dateTimeMinTimeSpanTicks = effectiveMinMonotonicDateTime.Ticks;
            
#if DEBUG
            DateTime confirmedMax = new DateTime(dateTimeMaxTimeSpanTicks, DateTimeKind.Utc);
            DateTime confirmedMin = new DateTime(dateTimeMinTimeSpanTicks, DateTimeKind.Utc);

            Debug.Assert(confirmedMax == effectiveMaxMonotonicDateTime);
            Debug.Assert(confirmedMin == effectiveMinMonotonicDateTime);
#endif


            TimeSpan maxDurationOffsetAsTimeSpan = TimeSpan.FromTicks(dateTimeMaxTimeSpanTicks - dateTimeUtcReferenceTicks);
            TimeSpan minDurationOffsetAsTimeSpan = TimeSpan.FromTicks(dateTimeMinTimeSpanTicks - dateTimeUtcReferenceTicks);

#if DEBUG
            DateTime maxDateTime = new DateTime((TimeSpan.FromTicks(dateTimeUtcReferenceTicks) + maxDurationOffsetAsTimeSpan).Ticks, DateTimeKind.Utc);
            DateTime minDateTime = new DateTime((TimeSpan.FromTicks(dateTimeUtcReferenceTicks) + minDurationOffsetAsTimeSpan).Ticks, DateTimeKind.Utc);
            Debug.WriteLine($"Max datetime: {maxDateTime:O}; Min datetime: {minDateTime:O}");
#endif

            Duration maxDurationOffset = (Duration) maxDurationOffsetAsTimeSpan;
            Duration minDurationOffset = (Duration) minDurationOffsetAsTimeSpan;

            Debug.Assert((TimeSpan) maxDurationOffset <= maxDurationOffsetAsTimeSpan, "Somehow round tripping messed us up!");
            Debug.Assert((TimeSpan) minDurationOffset >= minDurationOffsetAsTimeSpan, "Somehow round tripping messed us up!");

            Int128 maxDsAsTsTicks = ConvertStopwatchTicksToTimespanTicks(maxDurationOffset.Ticks);
            Int128 minDsAsTsTics = ConvertStopwatchTicksToTimespanTicks(minDurationOffset.Ticks);

            
            //monotonic range is limited to representable by long even if duration may not be.
            bool tooBigForMonoTs = maxDurationOffset.Ticks > long.MaxValue;
            bool tooSmallForMonoTs = minDurationOffset.Ticks < long.MinValue;


            if (maxDsAsTsTicks > long.MaxValue)
            {
                maxDurationOffset = Duration.FromStopwatchTicks(long.MaxValue);
            }

            if (minDsAsTsTics < long.MinValue)
            {
                minDurationOffset = Duration.FromStopwatchTicks(long.MinValue);
            }

            TimeSpan roundTrippingMin = (TimeSpan) minDurationOffset;
            TimeSpan roundTrippingMax = (TimeSpan) maxDurationOffset;
            //Debug.Assert(roundTrippingMin <= minDurationOffsetAsTimeSpan);
            Debug.Assert(ValidateMin(roundTrippingMin, minDurationOffsetAsTimeSpan));
            Debug.Assert(ValidateMax(roundTrippingMax, maxDurationOffsetAsTimeSpan));

            Debug.Assert((TimeSpan)maxDurationOffset <= maxDurationOffsetAsTimeSpan, "Somehow round tripping messed us up!");
            Debug.Assert((TimeSpan)minDurationOffset >= minDurationOffsetAsTimeSpan, "Somehow round tripping messed us up!");

            MonotonicTimeStamp<TStampContext> max =
                new MonotonicTimeStamp<TStampContext>(tooBigForMonoTs ? long.MaxValue : (long) maxDurationOffset.Ticks);
            MonotonicTimeStamp<TStampContext> min =
                new MonotonicTimeStamp<TStampContext>( tooSmallForMonoTs ? long.MinValue : (long) minDurationOffset.Ticks);
            return (max.ToUtcDateTime(), min.ToUtcDateTime(), max, min);

            static bool ValidateMin(TimeSpan test, TimeSpan testRef)
            {
                TimeSpan tolerance = StatContext.EasyConversionToAndFromTimespanTicks
                    ? TimeSpan.Zero
                    : TimeSpan.FromMilliseconds(1);

                TimeSpan diff = test <= testRef ? TimeSpan.Zero : test - testRef;
                return diff <= tolerance;
            }

            static bool ValidateMax(TimeSpan test, TimeSpan testRef)
            {
                TimeSpan tolerance = StatContext.EasyConversionToAndFromTimespanTicks
                    ? TimeSpan.Zero
                    : TimeSpan.FromMilliseconds(1);

                TimeSpan diff = test >= testRef ? TimeSpan.Zero : (test - testRef).Duration();
                return diff <= tolerance;
            }
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
        public DateTime ToUtcDateTime()
        {

            var ret =  DateTime.SpecifyKind(UtcReference + ((TimeSpan)ElapsedSinceUtcReference), DateTimeKind.Utc);
            return ret;
        }

        

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

        /// <summary>
        /// Convert this value to a portable timestamp suitable
        /// for serialization / using across process boundaries
        /// </summary>
        /// <returns>A portable timestamp.</returns>
        [Pure]
        public PortableMonotonicStamp ToPortableStamp()
            => MonoPortableConversionHelper<TStampContext>.ConvertToPortableMonotonicStamp(this);
        

        /// <summary>
        /// Convert 
        /// </summary>
        /// <param name="stamp"></param>
        /// <returns></returns>
        public static MonotonicTimeStamp<TStampContext> ImportPortableTimestamp(in PortableMonotonicStamp stamp)
        {
            //DateTime convertedDt = (DateTime)stamp;
            //PortableMonotonicStamp roundTripped = (PortableMonotonicStamp)convertedDt;
            //PortableDuration difference = roundTripped - stamp;
            //Debug.WriteLine(difference);


            return MonoPortableConversionHelper<TStampContext>.ConvertPortableToMonotonic(in stamp);

            ////todo fixit investigate
            //Int128 nanosecondsSinceUtcEpoch =
            //    ConvertNanosecondsToStopwatchTicks(stamp._dateTimeNanosecondOffsetFromMinValueUtc);
            //Int128 refTimeAsUtcNanosecondsSinceEpoch =
            //    ConvertDateTimeToNanosecondsSinceUtcEpoch(StatContext.UtcDateTimeBeginReference);
            //Int128 newNanosecondsOffset = nanosecondsSinceUtcEpoch - refTimeAsUtcNanosecondsSinceEpoch;
            //Int128 stopwatchTicksSinceReferenceTimeUtc = ConvertNanosecondsToStopwatchTicks(nanosecondsSinceUtcEpoch);
            //Duration timeSinceLocalTime = Duration.FromStopwatchTicks(stopwatchTicksSinceReferenceTimeUtc - StatContext.UtcLocalTimeOffsetAsDuration._ticks);
            //return CreateFromRefTicks((long)timeSinceLocalTime._ticks);
        }
        
         internal static Duration ConvertNanosecondsToDuration(in Int128 nanoSeconds)
        {
            Int128 stopwatchTicks = nanoSeconds * StatContext.NanosecondsFrequency / StatContext.TicksPerSecond;
            return new Duration(in stopwatchTicks);
        }

         internal static Int128 ConvertDateTimeToNanosecondsSinceUtcEpoch(DateTime dt)
         {
             Int128 stopwatchTicks = ConvertDateTimeToStopwatchTicksSinceEpoch(dt.ToUniversalTime());
             return stopwatchTicks * StatContext.NanosecondsFrequency / StatContext.TicksPerSecond;
         }
        internal static Int128 ConvertDateTimeToStopwatchTicksSinceEpoch(DateTime dt)
        {
            dt = dt.ToUniversalTime();
            long tsTicks = (dt - DateTime.MinValue.ToUniversalTime()).Ticks;
            return ConvertTimeSpanTicksToStopwatchTicks(tsTicks);
        }

        internal DateTime ConvertStopwatchTicksToDateTime(in Int128 stopwatchTicksSinceDtEpochUtc)
        {
            long timespanTicksSinceEpoch = (long) ConvertStopwatchTicksToTimespanTicks(stopwatchTicksSinceDtEpochUtc);
            TimeSpan ts = TimeSpan.FromTicks(timespanTicksSinceEpoch);
            return (DateTime.MinValue.ToUniversalTime() + ts);
        }
        
        internal static Int128 ConvertStopwatchTicksToTimespanTicks(in Int128 stopwatchTicks) =>
            stopwatchTicks * TimeSpan.TicksPerSecond / StatContext.TicksPerSecond;
        
        internal static Int128 ConvertTimeSpanTicksToStopwatchTicks(in Int128 timespanTicks) =>
            timespanTicks * StatContext.TicksPerSecond / TimeSpan.TicksPerSecond;
        
        internal static Int128 ConvertNanosecondsToStopwatchTicks(in Int128 nanoseconds)
        {
            Int128 stopwatchTicks = nanoseconds * StatContext.TicksPerSecond / StatContext.NanosecondsFrequency;
            return stopwatchTicks;
        }
        
        internal TimeSpan ConvertNanosecondsToTimespan(in Int128 nanoseconds)
        {
            Int128 timespanTicks = ConvertNanosecondsToTimespanTicks(nanoseconds);
            if (timespanTicks > long.MaxValue || timespanTicks < long.MinValue)
                throw new ArgumentOutOfRangeException(nameof(nanoseconds), nanoseconds,
                    ($"Nanoseconds specified  will not fit in a {nameof(TimeSpan)}."));
            return TimeSpan.FromTicks((long) timespanTicks);
        }
        
        internal Int128 ConvertNanosecondsToTimespanTicks(in Int128 nanoseconds)
        {
            return nanoseconds * (TimeSpan.TicksPerSecond / Context.NanosecondsFrequency);
        }
        
        internal Int128 ConvertTimeSpanTicksToNanoSeconds(in Int128 timespanTicks)
        {
            return timespanTicks * ((Int128) Context.NanosecondsFrequency) / TimeSpan.TicksPerSecond;
        }

        internal Int128 ConvertStopwatchTicksToNanoseconds(in Int128 stopwatchTicks)
        {
            return stopwatchTicks * ((Int128) Context.NanosecondsFrequency) / Context.TicksPerSecond;
        }

        internal Int128 ConvertDurationToNanoseconds(in Duration d)
        {
            return ConvertStopwatchTicksToNanoseconds(in d._ticks);
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