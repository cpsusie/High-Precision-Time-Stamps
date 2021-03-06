﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
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
        #region Static Values
        /// <summary>
        /// Number of nanoseconds in a second
        /// </summary>
        public const long NanosecondsFrequency = 1_000_000_000;

        /// <summary>
        /// Minimum value representable 
        /// </summary>
        public static ref readonly PortableMonotonicStamp MinValue => ref TheMinValue;
        /// <summary>
        /// Maximum value representable
        /// </summary>
        public static ref readonly PortableMonotonicStamp MaxValue => ref TheMaxValue;
        #endregion

        #region Conversion Operators
        /// <summary>
        /// Convert a monotonic stamp to a portable stamp
        /// </summary>
        /// <param name="monotonicStamp">the monotonic stamp to convert</param>
        /// <remarks>Will be roundtrippable (less timezone info ... will be made UTC)
        /// unless the factors for conversion between monotonic stamps and nanoseconds are
        /// not conducive to even division.  <see cref="MonotonicStampContext.EasyConversionToAndFromNanoseconds"/>.</remarks>
        public static explicit operator PortableMonotonicStamp(
            in MonotonicTimeStamp<MonotonicStampContext> monotonicStamp)
        {
            var (utcReferenceTime, offsetFromReference, _) = monotonicStamp.Value;
            Debug.Assert(utcReferenceTime.Kind == DateTimeKind.Utc);
            Int128 refTimeNanosecondsSinceMin = (((Int128)utcReferenceTime.Ticks * 100) - MinValueUtcDtNanoseconds);
            PortableDuration pd = (PortableDuration)offsetFromReference;
            return new PortableMonotonicStamp(pd._ticks + refTimeNanosecondsSinceMin);
        }

        /// <summary>
        /// Convert a date time to a portable timestamp
        /// </summary>
        /// <param name="convertMe">value to convert</param>
        /// <returns>converted value</returns>
        /// <remarks>If the source is a <see cref="DateTime"/>, will be roundtrippable back (with timezone locality stripped) </remarks>
        public static implicit operator PortableMonotonicStamp(DateTime convertMe)
        {
            Int128 ticksSinceUtcDotNetEpoch =
                ((Int128)convertMe.ToUniversalTime().Ticks * 100) - MinValueUtcDtNanoseconds;
            Debug.Assert(ticksSinceUtcDotNetEpoch >= MinValueUtcDtNanoseconds && ticksSinceUtcDotNetEpoch <= MaxValueUtcDtNanoseconds);
            return new PortableMonotonicStamp(in ticksSinceUtcDotNetEpoch);
        }

        /// <summary>
        /// Convert a monotonic stamp into a date time
        /// </summary>
        /// <param name="monotonicStamp"></param>
        public static explicit operator DateTime(in PortableMonotonicStamp monotonicStamp)
        {
            if (monotonicStamp._dateTimeNanosecondOffsetFromMinValueUtc < MinValueUtcDtNanoseconds ||
                monotonicStamp._dateTimeNanosecondOffsetFromMinValueUtc > MaxValueUtcDtNanoseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(monotonicStamp), monotonicStamp,
                    "The portable monotonic stamp is too big or too small to be expressed as a datetime.");
            }
            Int128 elapsedSinceEpoch = (MinValueUtcDtNanoseconds + monotonicStamp._dateTimeNanosecondOffsetFromMinValueUtc) / 100;
            Debug.Assert(elapsedSinceEpoch >= long.MinValue && elapsedSinceEpoch <= long.MaxValue);
            return new DateTime((long)elapsedSinceEpoch, DateTimeKind.Utc);
        } 
        #endregion

        /// <summary>
        /// Amount of time in nanoseconds since the .NET UTC Epoch.
        /// </summary>
        /// <remarks>Value of .NET UTC epoch is calculated by calling <see cref="DateTime.ToUniversalTime"/>
        /// on <see cref="DateTime.MinValue"/></remarks>
        public string NanosecondsSinceUtcEpoch =>
            $"{_dateTimeNanosecondOffsetFromMinValueUtc:N} nanoseconds since epoch.";

        /// <summary>
        /// The year (A.D. 0001 - 9999)
        /// </summary>
        public int Year => GetComponents().Year;
        /// <summary>
        /// The month (1-12)
        /// </summary>
        public int Month => GetComponents().Month;
        /// <summary>
        /// The day (1-28, 1-29, 1-30, 1-31 depending on <see cref="Month"/>)
        /// </summary>
        public int Day => GetComponents().Day;
        /// <summary>
        /// The hour (0-23)
        /// </summary>
        public int Hour => GetComponents().Hour;
        /// <summary>
        /// Then minutes (0-59)
        /// </summary>
        public int Minutes => GetComponents().Minute;
        /// <summary>
        /// The seconds (0-59)
        /// </summary>
        public int Seconds => GetComponents().WholeSeconds;
        /// <summary>
        /// The fractional seconds (1 - 9,999,999)
        /// </summary>
        public int FractionalSeconds => GetFractionalSeconds();

        /// <summary>
        /// Amount of time elapsed since epoch
        /// </summary>
        public PortableDuration TimeSinceEpoch => new PortableDuration(in _dateTimeNanosecondOffsetFromMinValueUtc);

        #region CTORS and related
        internal PortableMonotonicStamp(in Int128 nanosecondSinceDtUtcEpoch)
        {
            if (nanosecondSinceDtUtcEpoch > MaxValueUtcDtNanoseconds ||
                nanosecondSinceDtUtcEpoch < MinValueUtcDtNanoseconds)
                throw new ArgumentOutOfRangeException(nameof(nanosecondSinceDtUtcEpoch),
                    nanosecondSinceDtUtcEpoch.ToString("N"),
                    "The offset supplied places this stamp outside the supported range of .NET time.");
            _dateTimeNanosecondOffsetFromMinValueUtc = nanosecondSinceDtUtcEpoch;
        }

        static PortableMonotonicStamp()
        {
            MaxValueUtcDtNanoseconds = DateTime.MaxValue.ToUniversalTime().Ticks * (Int128)100;
            MinValueUtcDtNanoseconds = DateTime.MinValue.ToUniversalTime().Ticks * (Int128)100;
            TheMinValue = new PortableMonotonicStamp(MinValueUtcDtNanoseconds);
            TheMaxValue = new PortableMonotonicStamp(MaxValueUtcDtNanoseconds);
            Validate();

        }
        [Conditional("DEBUG")] // DEBUG they are used // RELEASE the method doesn't get called
        [SuppressMessage("ReSharper", "RedundantAssignment")] 
        static void Validate()
        {
            Debug.Assert(PortableDuration.TicksPerSecond == 1_000_000_000);
            Debug.Assert(TimeSpan.TicksPerSecond == 10_000_000);
            Int128 minValueNanoseconds = MinValue._dateTimeNanosecondOffsetFromMinValueUtc;
            Int128 maxValueNanoseconds = MaxValue._dateTimeNanosecondOffsetFromMinValueUtc;

            Int128 toTimeSpanTicksMin = minValueNanoseconds / 100;
            Int128 toTimeSpanTicksMax = maxValueNanoseconds / 100;
            Debug.Assert(toTimeSpanTicksMin >= long.MinValue);
            Debug.Assert(toTimeSpanTicksMax <= long.MaxValue);
            DateTime min = new DateTime((long)toTimeSpanTicksMin, DateTimeKind.Utc);
            DateTime max = new DateTime((long)toTimeSpanTicksMax, DateTimeKind.Utc);
            Debug.Assert(min == DateTime.MinValue.ToUniversalTime() && max == 
                DateTime.MaxValue.ToUniversalTime());
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Convert this to a datetime in the local datetime timezone.
        /// </summary>
        /// <returns>A local datetime</returns>
        /// <exception cref="PortableTimestampOverflowException">Cannot be converted to
        /// DateTime because of overflow.</exception>
        [System.Diagnostics.Contracts.Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ToLocalDateTime()
        {
            try
            {
                return ToUtcDateTime().ToLocalTime();
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
                return (DateTime)this;
            }
            catch (ArgumentOutOfRangeException inner)
            {
                throw new PortableTimestampOverflowException("Overflow prevented conversion of portable monotonic stamp to a UTC DateTime.", inner);
            }
        }

        /// <summary>
        /// Get a string representation of this value in ISO 8601 format
        /// in UTC.
        /// </summary>
        /// <returns>A string representation.</returns>
        public override string ToString() => BuildString(false);
        
        /// <summary>
        /// Get a string representation of this value in ISO 8601 format
        /// in UTC.
        /// </summary>
        /// <returns>A string representation.</returns>
        public string ToLocalString() => BuildString(true);
        #endregion
        
        #region Equality/Comparison and Related Methods and Operators
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
        #endregion

        #region Arithmetic operators

        /// <summary>
        /// Adds a duration to a stamp, yielding a stamp
        /// </summary>
        /// <param name="ms">the stamp addend</param>
        /// <param name="d">the duraton addend</param>
        /// <returns>the sum</returns>
        /// <exception cref="PortableTimestampOverflowException">Caused overflow</exception>
        public static PortableMonotonicStamp operator +(in PortableMonotonicStamp ms, in PortableDuration d)
        {
            Int128 sum = ms._dateTimeNanosecondOffsetFromMinValueUtc + d._ticks;
            if (sum >= MinValueUtcDtNanoseconds && sum <= MaxValueUtcDtNanoseconds)
            {
                return new PortableMonotonicStamp(in sum);
            }
            throw new PortableTimestampOverflowException("The sum caused overflow.");
        }

        /// <summary>
        /// Adds a duration to a stamp, yielding a stamp
        /// </summary>
        /// <param name="ms">the stamp addend</param>
        /// <param name="d">the duraton addend</param>
        /// <returns>the sum</returns>
        /// <exception cref="PortableTimestampOverflowException">Caused overflow</exception>        
        public static PortableMonotonicStamp operator +(in PortableDuration d, in PortableMonotonicStamp ms) 
            => ms + d;
        
        /// <summary>
        /// Adds a timespan to a stamp, yielding a stamp
        /// </summary>
        /// <param name="ms">the stamp addend</param>
        /// <param name="ts">the timespan addend</param>
        /// <returns>the sum</returns>
        /// <exception cref="PortableTimestampOverflowException">Caused overflow</exception>
        public static PortableMonotonicStamp operator +(in PortableMonotonicStamp ms, TimeSpan ts)
        {
            Int128 sum = ms._dateTimeNanosecondOffsetFromMinValueUtc + ((Int128) ts.Ticks * 100);
            if (sum >= MinValueUtcDtNanoseconds && sum <= MaxValueUtcDtNanoseconds)
            {
                return new PortableMonotonicStamp(in sum);
            }
            throw new PortableTimestampOverflowException("The sum caused overflow.");
        }

        /// <summary>
        /// Adds a duration to a stamp, yielding a stamp
        /// </summary>
        /// <param name="ms">the stamp addend</param>
        /// <param name="ts">the timespan addend</param>
        /// <returns>the sum</returns>
        /// <exception cref="PortableTimestampOverflowException">Caused overflow</exception>        
        public static PortableMonotonicStamp operator +(TimeSpan ts, in PortableMonotonicStamp ms) 
            => ms + ts;
        /// <summary>
        /// Subtracts a duration from a stamp, yielding a stamp
        /// </summary>
        /// <param name="minuend">Minuend</param>
        /// <param name="subtrahend">the subtrahend</param>
        /// <returns>the sum</returns>
        /// <exception cref="PortableTimestampOverflowException">Caused overflow</exception>
        public static PortableMonotonicStamp operator -(in PortableMonotonicStamp minuend, in PortableDuration subtrahend)
        {
            Int128 sum = minuend._dateTimeNanosecondOffsetFromMinValueUtc + subtrahend._ticks;
            if (sum >= MinValueUtcDtNanoseconds && sum <= MaxValueUtcDtNanoseconds)
            {
                return new PortableMonotonicStamp(in sum);
            }
            throw new PortableTimestampOverflowException("The sum caused overflow.");
        }
        
        /// <summary>
        /// Subtracts a timespan from a stamp, yielding a stamp
        /// </summary>
        /// <param name="minuend">Minuend</param>
        /// <param name="subtrahend">the subtrahend</param>
        /// <returns>the sum</returns>
        /// <exception cref="PortableTimestampOverflowException">Caused overflow</exception>
        public static PortableMonotonicStamp operator -(in PortableMonotonicStamp minuend, TimeSpan subtrahend)
        {
            Int128 difference = minuend._dateTimeNanosecondOffsetFromMinValueUtc - ((Int128) subtrahend.Ticks * 100);
            if (difference >= MinValueUtcDtNanoseconds && difference <= MaxValueUtcDtNanoseconds)
            {
                return new PortableMonotonicStamp(in difference);
            }
            throw new PortableTimestampOverflowException("The subtraction caused overflow.");
        }
        
        /// <summary>
        /// Subtracts a stamp from another stamp yielding the duration between them.
        /// </summary>
        /// <param name="minuend">the minuend</param>
        /// <param name="subtrahend">the subtrahend</param>
        /// <returns>the duration between the stamps</returns>
        public static PortableDuration operator -(in PortableMonotonicStamp minuend,
            in PortableMonotonicStamp subtrahend)
        {
            Int128 ticksDiff = minuend._dateTimeNanosecondOffsetFromMinValueUtc -
                               subtrahend._dateTimeNanosecondOffsetFromMinValueUtc;
            return new PortableDuration(in ticksDiff);
        }

        /// <summary>
        /// Subtracts a DateTime from a stamp yielding the duration between them.
        /// </summary>
        /// <param name="minuend">the minuend</param>
        /// <param name="subtrahend">the subtrahend</param>
        /// <returns>the duration between the stamps</returns>
        public static PortableDuration operator -(in PortableMonotonicStamp minuend,
            DateTime subtrahend) => minuend - (PortableMonotonicStamp) subtrahend;

        /// <summary>
        /// Subtract a portable stamp from a date time yielding the duration between them
        /// </summary>
        /// <param name="minuend">the minuend</param>
        /// <param name="subtrahend">the subtrahend</param>
        /// <returns>the difference</returns>
        public static PortableDuration operator -(DateTime minuend, in PortableMonotonicStamp subtrahend) =>
            ((PortableMonotonicStamp) minuend) - subtrahend;
        
        #endregion
        #region Private Methods
        
        private string BuildString(bool local)
        { 
//          0	1	2	3	4	5	6	7	8	9	10	11	12	13	14	15	16	17	18	19	20	
//          2	0	2	0	-	1	2	-	1	2	T	1	8	:	2	1	:	4	3	.	2	


//          21	22	23	24	25	26	27	28	29	30	31	32
//          5	9	2	9	0	8	-	0	5	:	0	0

            const int insertBeforeIdx = 27;
            //if we have non-zero values (from nanoseconds vs datetime/timespan's tenth of a microsecond resolution), fill them in
            //otherwise, don't waste space.  Maybe reconsider this later. ... want always same width?  ... want specify width?
            //
            var result = new StringBuilder((local ? ToLocalDateTime() : ToUtcDateTime()).ToString("O"));
            int fractionalSeconds = GetFractionalSeconds() % 100;
            int penultimateChar = Math.DivRem(fractionalSeconds, 10, out int ultimateChar);
            if (ultimateChar != 0)
            {
                string insertMe = penultimateChar == 0
                    ? "0" + fractionalSeconds
                    : fractionalSeconds.ToString();
                result.Insert(insertBeforeIdx, insertMe);
            }
            else if (penultimateChar != 0)
            {
                result.Insert(insertBeforeIdx, penultimateChar.ToString());
            }
            
            return result.ToString();
        }

        private (short Year, byte Month, byte Day, byte Hour, byte Minute, short WholeSeconds) GetComponents()
        {
            DateTime utc = ToUtcDateTime();
            short year, wholeSeconds;
            byte month, day, hour, minute;
            unchecked
            {
                year = (short)utc.Year;
                month = (byte) utc.Month;
                day = (byte) utc.Day;
                hour = (byte) utc.Hour;
                minute = (byte) utc.Minute;
                wholeSeconds = (short) utc.Second;

//                (Int128 wholeSecondsFromStamp, Int128 fractionalSecondsStamp) =
//                    Int128.DivRem(in _dateTimeNanosecondOffsetFromMinValueUtc, NanosecondsFrequency);
//#if DEBUG
//                int wsfs = (int) (wholeSecondsFromStamp % 60);
//                Debug.Assert(wsfs == wholeSeconds);
//#endif
//                fractionalSeconds = (int) fractionalSecondsStamp;
            }
            return (year, month, day, hour, minute, wholeSeconds);
        }

        private int GetFractionalSeconds()
        {
            (Int128 _, Int128 fractionalSecondsStamp) =
                    Int128.DivRem(in _dateTimeNanosecondOffsetFromMinValueUtc, NanosecondsFrequency);
            return (int)fractionalSecondsStamp;
        }
        #endregion

        #region Private Data
        // ReSharper disable once InconsistentNaming -- internal where private normal for efficiency
        [DataMember] internal readonly Int128 _dateTimeNanosecondOffsetFromMinValueUtc;
        private static readonly Int128 MaxValueUtcDtNanoseconds;
        private static readonly Int128 MinValueUtcDtNanoseconds;
        private static readonly PortableMonotonicStamp TheMinValue;
        private static readonly PortableMonotonicStamp TheMaxValue;
        #endregion
    }
}