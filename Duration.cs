//Original version (before CJM Screws LLC made modifications)
//taken from https://raw.githubusercontent.com/dotnet/runtime/6072e4d3a7a2a1493f514cdf4be75a3d56580e84/src/libraries/System.Private.CoreLib/src/System/TimeSpan.cs
//that was licensed to the .NET Foundation by its original author.  In turn, the .NET Foundation licensed this code to CJM Screws, LLC under the MIT 
//license.  CJM Screws LLC licenses the version as modified to you under the MIT license.  CJM Screws LLC claims copyright to the modifications made 
//to this class, but makes no claim to the unaltered original.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using TickInt = HpTimeStamps.BigMath.Int128;
[assembly: InternalsVisibleTo("UnitTests")]
namespace HpTimeStamps
{
    /// <summary>
    /// Based on <see cref="TimeSpan"/> except <see cref="TicksPerSecond"/> is based on <see cref="Stopwatch.Frequency"/>
    /// rather than TimeSpan's.
    /// </summary>
    /// <remarks>Because <see cref="Stopwatch.Frequency"/> is different in different environments, this duration should not be considered
    /// portable or usable across process boundaries.  DO NOT SERIALIZE OR USE ACROSS PROCESS BOUNDARIES.  Use </remarks>
    public readonly struct Duration : IComparable<Duration>, IEquatable<Duration>
    {
        #region Readonly Public Static Values
        /// <summary>
        /// The smallest stopwatch frequency supported by this library.
        /// </summary>
        public const long MinimumSupportedStopwatchTicksPerSecond = 1_000;
        /// <summary>
        /// Number of ticks in a millisecond
        /// </summary>
        public static readonly long TicksPerMillisecond;
        /// <summary>
        /// Number of ticks in a second.  Unlike <see cref="TimeSpan"/>, this will always
        /// be keyed to <see cref="Stopwatch.Frequency"/>.
        /// </summary>
        public static readonly long TicksPerSecond;
        /// <summary>
        /// Number of ticks per minute
        /// </summary>
        public static readonly long TicksPerMinute;
        /// <summary>
        /// Number of ticks per hour.
        /// </summary>
        public static readonly long TicksPerHour;
        /// <summary>
        /// Number of ticks per day 
        /// </summary>
        public static readonly long TicksPerDay;
        
        /// <summary>
        /// Zero
        /// </summary>
        public static readonly Duration Zero = new Duration(0);
        /// <summary>
        /// Maximum value of a duration
        /// </summary>
        public static readonly Duration MaxValue = new Duration(TickInt.MaxValue);
        /// <summary>
        /// Minimum value of a duration
        /// </summary>
        public static readonly Duration MinValue = new Duration(TickInt.MinValue);
        #endregion

        #region Readonly internal static values
        internal static readonly TickInt TicksPerMicrosecond;
        
        /// <summary>
        /// Longest positive period representable in seconds
        /// </summary>
        internal static readonly TickInt MaxSeconds;
        /// <summary>
        /// Longest negative period representable in seconds
        /// </summary>
        internal static readonly TickInt MinSeconds;
        /// <summary>
        /// Longest positive period represented in milliseconds
        /// </summary>
        internal static readonly TickInt MaxMilliseconds;
        /// <summary>
        /// Longest negative period represented in millisecond
        /// </summary>
        internal static readonly TickInt MinMilliseconds;
        /// <summary>
        /// Number of ticks in a tenth of a second
        /// </summary>
        internal static readonly TickInt TicksPerTenthSecond;
        /// <summary>
        /// Amount to shift a <see cref="TickInt"/> right to get its sign bit.
        /// </summary>
        internal const int TickIntRightShiftGetSignBitAmount = 127;
        #endregion

        #region Conversion Operators
        /// <summary>
        /// Convert a timespan into a duration
        /// </summary>
        /// <param name="convertMe">value to convert</param>
        /// <exception cref="OverflowException">Can't fit.</exception>
        public static explicit operator Duration(TimeSpan convertMe)
        {
            TickInt swTicks = ConvertTimespanTicksToStopwatchTicks(convertMe.Ticks);
            return new Duration(in swTicks);
        }

        /// <summary>
        /// Convert a duration into a timespan
        /// </summary>
        /// <param name="convertMe">value to convert</param>
        /// <exception cref="OverflowException">Can't fit.</exception>
        public static explicit operator TimeSpan(in Duration convertMe)
        {
            long timeSpanTicks = ConvertStopwatchTicksToTimespanTicks(in convertMe._ticks);
            return TimeSpan.FromTicks(timeSpanTicks);
        }
        #endregion

        #region Factory Methods
        /// <summary>
        /// Convert timespan ticks into a duration
        /// </summary>
        /// <param name="timespanTicks">the timespan ticks</param>
        /// <returns>a duration</returns>
        public static Duration FromTimespanTicks(long timespanTicks)
        {
            TickInt swTicks = ConvertTimespanTicksToStopwatchTicks(timespanTicks);
            return new Duration(in swTicks);
        }
        /// <summary>
        /// Compute a duration from a value representing days
        /// </summary>
        /// <param name="value">Value representing days</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a Duration.</exception>
        public static Duration FromDays(double value) => Interval(value, TicksPerDay);
        /// <summary>
        /// Compute a duration from a value representing hours
        /// </summary>
        /// <param name="value">Value representing days</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a Duration.</exception>
        public static Duration FromHours(double value) => Interval(value, TicksPerHour);
        /// <summary>
        /// Compute a duration from a value representing milliseconds
        /// </summary>
        /// <param name="value">Value representing milliseconds</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a Duration.</exception>
        public static Duration FromMilliseconds(double value) => Interval(value, TicksPerMillisecond);
        /// <summary>
        /// Get a Duration from microseconds
        /// </summary>
        /// <param name="value">Microseconds</param>
        /// <returns>A duration consisting of <paramref name="value"/> microseconds.</returns>
        public static Duration FromMicroseconds(double value) => Interval(value, (double) TicksPerMicrosecond);
        /// <summary>
        /// Compute a duration from a value representing minutes
        /// </summary>
        /// <param name="value">Value representing minutes</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a Duration.</exception>
        public static Duration FromMinutes(double value) => Interval(value, TicksPerMinute);
        /// <summary>
        /// Compute a duration from a value representing seconds
        /// </summary>
        /// <param name="value">Value representing seconds</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a Duration.</exception>
        public static Duration FromSeconds(double value) => Interval(value, TicksPerSecond);
        /// <summary>
        /// Create a duration from ticks
        /// </summary>
        /// <param name="value">ticks</param>
        /// <returns>the value</returns>
        internal static Duration FromStopwatchTicks(in TickInt value) => new Duration(in value);
        #endregion

        #region Public Properties
        /// <summary>
        /// Stopwatch ticks
        /// </summary>
        internal TickInt Ticks => _ticks;

        /// <summary>
        /// Number of whole days represented, fractional time remaining discarded.
        /// </summary>
        public int Days => (int)(_ticks / TicksPerDay);

        /// <summary>
        /// Number of whole hours represented, fractional time remaining discarded
        /// </summary>
        public int Hours => (int)((_ticks / TicksPerHour) % 24);

        /// <summary>
        /// Number of whole milliseconds represented, fractional time remaining discarded
        /// </summary>
        public int Milliseconds => (int)((_ticks / TicksPerMillisecond) % 1_000);

        /// <summary>
        /// Number of whole microseconds represented, fractional time remaining discarded
        /// </summary>
        public long Microseconds => (long) ((_ticks / TicksPerMicrosecond) % 1_000_000);

        /// <summary>
        /// Number of whole minutes represented, fractional time remaining discarded
        /// </summary>
        public int Minutes => (int)((_ticks / TicksPerMinute) % 60);

        /// <summary>
        /// Number of whole seconds represented, fractional time remaining discarded
        /// </summary>
        public int Seconds => (int)((_ticks / TicksPerSecond) % 60);

        /// <summary>
        /// The duration represented in days, including fractional parts
        /// </summary>
        public double TotalDays => ((double)_ticks) / TicksPerDay;

        /// <summary>
        /// The duration represented in hours, including fractional parts
        /// </summary>
        public double TotalHours => (double)_ticks / TicksPerHour;

        /// <summary>
        /// Duration represented in microseconds, including fractional parts
        /// </summary>
        public double TotalMicroseconds =>  (double) _ticks / (double) TicksPerMicrosecond;

        /// <summary>
        /// The duration represented in milliseconds, including fractional parts
        /// </summary>
        [SuppressMessage("ReSharper", "RedundantCast")]
        public double TotalMilliseconds
        {
            get
            {
                double temp = (double)_ticks / TicksPerMillisecond;
                if (temp > (double) MaxMilliseconds)
                    return (double)MaxMilliseconds;

                if (temp < (double) MinMilliseconds)
                    return (double)MinMilliseconds;

                return temp;
            }
        }

        /// <summary>
        /// The duration represented in minutes, including fractional parts
        /// </summary>
        public double TotalMinutes => (double)_ticks / TicksPerMinute;
        /// <summary>
        /// The duration represented in seconds, including fractional parts
        /// </summary>
        public double TotalSeconds => (double)_ticks / TicksPerSecond;
        #endregion

        #region CTORS
        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="ticks">stopwatch ticks</param>
        internal Duration(in TickInt ticks) => _ticks = ticks;

        /// <summary>
        /// Create a duration from stopwatch ticks
        /// </summary>
        /// <param name="stopwatchTicks">the ticks</param>
        public Duration(long stopwatchTicks) => _ticks = stopwatchTicks; 

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="hours">Hours</param>
        /// <param name="minutes">Minutes</param>
        /// <param name="seconds">Seconds</param>
        /// <exception cref="ArgumentException">Period too long to fit.</exception>
        public Duration(int hours, int minutes, int seconds) => _ticks = TimeToTicks(hours, minutes, seconds);

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="days">days</param>
        /// <param name="hours">hours</param>
        /// <param name="minutes">minutes</param>
        /// <param name="seconds">seconds</param>
        /// <exception cref="ArgumentException">Period too long to fit</exception>
        public Duration(int days, int hours, int minutes, int seconds)
            : this(days, hours, minutes, seconds, 0) { }

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="days">days</param>
        /// <param name="hours">hours</param>
        /// <param name="minutes">minutes</param>
        /// <param name="seconds">seconds</param>
        /// <param name="milliseconds">milliseconds</param>
        /// <exception cref="ArgumentException">Period too long to fit.</exception>
        [SuppressMessage("ReSharper", "RedundantCast")]
        public Duration(int days, int hours, int minutes, int seconds, int milliseconds)
        {
            long totalMilliSeconds = ((long)days * 3_600 * 24 + (long)hours * 3_600 + (long)minutes * 60 + seconds) * 1_000 + milliseconds;
            if (totalMilliSeconds > MaxMilliseconds || totalMilliSeconds < MinMilliseconds)
                throw new ArgumentException("Period specified will not fit in a timespan.");
            _ticks = (TickInt)totalMilliSeconds * TicksPerMillisecond;
        }

        static Duration()
        {
            TicksPerSecond = Stopwatch.Frequency;
            if (TicksPerSecond < MinimumSupportedStopwatchTicksPerSecond)
            {
                throw new UnsupportedStopwatchResolutionException(TicksPerSecond,
                    MinimumSupportedStopwatchTicksPerSecond);
            }
            TicksPerMillisecond = TicksPerSecond / 1_000;
            TicksPerMicrosecond = (TickInt) TicksPerMillisecond / 1_000;
            TicksPerMinute = TicksPerSecond * 60;
            TicksPerHour = TicksPerMinute * 60;
            TicksPerDay = TicksPerHour * 24;
            MaxSeconds = TickInt.MaxValue / TicksPerSecond;
            MinSeconds = TickInt.MinValue / TicksPerSecond;
            MaxMilliseconds = TickInt.MaxValue / TicksPerMillisecond;
            MinMilliseconds = TickInt.MinValue / TicksPerMillisecond;
            TicksPerTenthSecond = TicksPerMillisecond * 100;
        }

        #endregion

        #region ToString Methods
        /// <summary>
        /// Get a string representation
        /// </summary>
        /// <returns>a string representation</returns>
        public override string ToString() => TotalMilliseconds.ToString("N6") + " milliseconds";
        #endregion

        #region Equatable and Comparable Methods and Operators
        /// <summary>
        /// Test two durations for equality
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if equal false otherwise</returns>
        public static bool operator ==(in Duration t1, in Duration t2) => t1._ticks == t2._ticks;
        /// <summary>
        /// Test two durations for inequality
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if not equal false otherwise</returns>
        public static bool operator !=(in Duration t1, in Duration t2) => t1._ticks != t2._ticks;
        /// <summary>
        /// Test two durations for to see if <paramref name="t1"/> is less than <paramref name="t2"/>
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if <paramref name="t1"/> is less than <paramref name="t2"/>, false otherwise</returns>
        public static bool operator <(in Duration t1, in Duration t2) => t1._ticks < t2._ticks;
        /// <summary>
        /// Test two durations for to see if <paramref name="t1"/> is less than or equal to <paramref name="t2"/>
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if <paramref name="t1"/> is less than or equal to <paramref name="t2"/>, false otherwise</returns>
        public static bool operator <=(in Duration t1, in Duration t2) => t1._ticks <= t2._ticks;
        /// <summary>
        /// Test two durations for to see if <paramref name="t1"/> is greater than <paramref name="t2"/>
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if <paramref name="t1"/> is greater than <paramref name="t2"/>, false otherwise</returns>
        public static bool operator >(in Duration t1, in Duration t2) => t1._ticks > t2._ticks;

        /// <summary>
        /// Test two durations for to see if <paramref name="t1"/> is greater than or equal to <paramref name="t2"/>
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if <paramref name="t1"/> is greater than or equal to <paramref name="t2"/>, false otherwise</returns>
        public static bool operator >=(in Duration t1, in Duration t2) => t1._ticks >= t2._ticks;
        /// <inheritdoc />
        public override bool Equals(object value) => value is Duration d && d == this;
        /// <inheritdoc />
        public bool Equals(Duration obj) => obj == this;
        /// <inheritdoc />
        public override int GetHashCode() => _ticks.GetHashCode();
        /// <summary>
        /// Compare this duration to another to establish the order between them
        /// </summary>
        /// <param name="otherValue">value to compare</param>
        /// <returns>
        /// A positive number if this value succeeds <paramref name="otherValue"/> in sort order.
        /// Zero if this value has the same position in the sort order as <paramref name="otherValue"/>.
        /// A negative number if this value precedes <paramref name="otherValue"/> in sort order.
        /// </returns>
        public int CompareTo(Duration otherValue) => Compare(in this, in otherValue);
        /// <summary>
        /// Compare two durations
        /// </summary>
        /// <param name="lhs">left hand comparand</param>
        /// <param name="rhs">right hand comparand</param>
        /// <returns>
        /// A positive number if <paramref name="lhs"/> succeeds <paramref name="rhs"/> in sort order.
        /// Zero if <paramref name="lhs"/> has the same position in the sort order as <paramref name="rhs"/>.
        /// A negative number if <paramref name="lhs"/> precedes <paramref name="rhs"/> in sort order.
        /// </returns>
        public static int Compare(in Duration lhs, in Duration rhs)
        {
            if (lhs._ticks > rhs._ticks) return 1;
            if (lhs._ticks < rhs._ticks) return -1;
            return 0;
        }
        #endregion

        #region Mathematical Methods and Operators
        /// <summary>
        /// Multiply this value by a specified factor
        /// </summary>
        /// <param name="factor">the factor</param>
        /// <returns>the product</returns>
        [System.Diagnostics.Contracts.Pure]
        public Duration Multiply(double factor) => this * factor;
        /// <summary>
        /// Divide this value by a specified divisor
        /// </summary>
        /// <param name="divisor">the divisor</param>
        /// <returns>the product</returns>
        [System.Diagnostics.Contracts.Pure]
        public Duration Divide(double divisor) => this / divisor;
        /// <summary>
        /// Divide this value by another duration
        /// </summary>
        /// <param name="ts">the divisor</param>
        /// <returns>the quotient</returns>
        [System.Diagnostics.Contracts.Pure]
        public double Divide(Duration ts) => this / ts;
        /// <summary>
        /// Subtract two durations
        /// </summary>
        /// <param name="t1">minuend</param>
        /// <param name="t2">subtrahend</param>
        /// <returns>difference</returns>
        /// <exception cref="OverflowException">Operation overflow</exception>
        public static Duration operator -(in Duration t1, in Duration t2) => t1.Subtract(in t2);
        /// <summary>
        /// Apply unary + operator
        /// </summary>
        /// <param name="t">operand</param>
        /// <returns>a value equal to <paramref name="t"/>/</returns>
        public static Duration operator +(in Duration t) => t;
        /// <summary>
        /// Add two addends
        /// </summary>
        /// <param name="t1">first addend</param>
        /// <param name="t2">second addend</param>
        /// <returns>sum</returns>
        /// <exception cref="OverflowException">Operation resulted in overflow</exception>
        public static Duration operator +(in Duration t1, in Duration t2) => t1.Add(t2);
        /// <summary>
        /// Multiply a duration by a factor
        /// </summary>
        /// <param name="factor">the factor</param>
        /// <param name="timeSpan">the duration</param>
        /// <returns>the product</returns>
        /// <exception cref="OverflowException">Operation resulted in overflow.</exception>
        public static Duration operator *(double factor, in Duration timeSpan) => timeSpan * factor;
        // Using floating-point arithmetic directly means that infinities can be returned, which is reasonable
        // if we consider TimeSpan.FromHours(1) / TimeSpan.Zero asks how many zero-second intervals there are in
        // an hour for which infinity is the mathematic correct answer. Having TimeSpan.Zero / TimeSpan.Zero return NaN
        // is perhaps less useful, but no less useful than an exception.
        /// <summary>
        /// Divide two durations
        /// </summary>
        /// <param name="t1">dividend</param>
        /// <param name="t2">divisor</param>
        /// <returns>quotient</returns>
        public static double operator /(in Duration t1, in Duration t2) => (double) t1.Ticks / (double)t2.Ticks;

        /// <summary>
        /// Multiply a duration 
        /// </summary>
        /// <param name="timeSpan">Duration factor</param>
        /// <param name="factor">factor</param>
        /// <returns>product</returns>
        /// <exception cref="ArgumentException"><paramref name="factor"/> was nan</exception>
        /// <exception cref="OverflowException">operation resulted in overflow</exception>
        public static Duration operator *(in Duration timeSpan, double factor)
        {
            if (double.IsNaN(factor))
            {
                throw new ArgumentException("NaN is not a valid value for parameter.", nameof(factor));
            }

            // Rounding to the nearest tick is as close to the result we would have with unlimited
            // precision as possible, and so likely to have the least potential to surprise.
            double ticks = Math.Round((double) timeSpan.Ticks * factor);
            return IntervalFromDoubleTicks(ticks);
        }

        /// <summary>
        /// Get the absolute value of this Duration
        /// </summary>
        /// <returns>the absolute value</returns>
        public Duration AbsoluteValue()
        {
            if (Ticks == MinValue.Ticks)
                throw new OverflowException("This value is the most negative value and has no positive 2's complement counterpart.");
            return new Duration(_ticks >= 0 ? _ticks : -_ticks);
        }

        /// <summary>
        /// Return the additive inverse of this value
        /// </summary>
        /// <returns>The additive inverse</returns>
        [System.Diagnostics.Contracts.Pure]
        public Duration Negate()
        {
            if (Ticks == MinValue.Ticks)
                throw new OverflowException("This value is the most negative value " +
                                            "possible and has no positive counterpart in a 2's complement representation.");
            return new Duration(-_ticks);
        }

        /// <summary>
        /// Add another value to this one
        /// </summary>
        /// <param name="ts">value to add to this one</param>
        /// <returns>sum</returns>
        /// <exception cref="OverflowException">addition caused overflow</exception>
        [System.Diagnostics.Contracts.Pure]
        public Duration Add(in Duration ts)
        {
            TickInt result = _ticks + ts._ticks;
            // Overflow if signs of operands was identical and result's
            // sign was opposite.
            // >> TickIntRightShiftGetSignBitAmount gives the sign bit (either 64 1's or 64 0's).
            if ((_ticks >> TickIntRightShiftGetSignBitAmount == ts._ticks >> TickIntRightShiftGetSignBitAmount) &&
                (_ticks >> TickIntRightShiftGetSignBitAmount != result >> TickIntRightShiftGetSignBitAmount))
                throw new OverflowException("The addition resulted in overflow.");
            return new Duration(result);
        }


        /// <summary>
        /// Subtract another value from this one
        /// </summary>
        /// <param name="ts">the subtrahend</param>
        /// <returns>the difference</returns>
        /// <exception cref="OverflowException">Result caused overflow.</exception>
        [System.Diagnostics.Contracts.Pure]
        public Duration Subtract(in Duration ts)
        {
            TickInt result = _ticks - ts._ticks;
            // Overflow if signs of operands was different and result's
            // sign was opposite from the first argument's sign.
            // >> TickIntRightShiftGetSignBitAmount gives the sign bit 
            if ((_ticks >> TickIntRightShiftGetSignBitAmount != ts._ticks >>
                    TickIntRightShiftGetSignBitAmount) &&
                (_ticks >> TickIntRightShiftGetSignBitAmount != result
                    >> TickIntRightShiftGetSignBitAmount))
                throw new OverflowException();
            return new Duration(result);
        }

        /// <summary>
        /// Negate the duration
        /// </summary>
        /// <param name="t">The duration to negate</param>
        /// <returns>the additive inverse</returns>
        public static Duration operator -(in Duration t)
        {
            if (t._ticks == MinValue._ticks)
                throw new OverflowException("The duration supplied has no positive 2's complement counterpart.");
            return new Duration(-t._ticks);
        }

        /// <summary>
        /// Divide a duration by a divisor
        /// </summary>
        /// <param name="timeSpan">the dividend</param>
        /// <param name="divisor">divisor</param>
        /// <returns>quotient</returns>
        /// <exception cref="ArgumentException"><paramref name="divisor"/> was <see cref="double.NaN"/>.</exception>
        public static Duration operator /(in Duration timeSpan, double divisor)
        {
            if (double.IsNaN(divisor))
            {
                throw new ArgumentException("NaN is not a valid value for parameter", nameof(divisor));
            }

            double ticks = Math.Round((double) timeSpan.Ticks / divisor);
            return IntervalFromDoubleTicks(ticks);
        }
        #endregion

        #region Private and Internal Helper Methods
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ConvertStopwatchTicksToTimespanTicks(long stopwatchTicks) =>
            (long) ( ((TickInt) stopwatchTicks) * MonotonicTimeStamp<MonotonicStampContext>.TheToTsTickConversionFactorNumerator) /
            MonotonicTimeStamp<MonotonicStampContext>.ToToTsTickConversionFactorDenominator;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ConvertTimespanTicksToStopwatchTicks(long timespanTicks) =>
            (long) (( (TickInt) timespanTicks * MonotonicTimeStamp<MonotonicStampContext>.ToToTsTickConversionFactorDenominator) /
            MonotonicTimeStamp<MonotonicStampContext>.TheToTsTickConversionFactorNumerator);
        internal static long ConvertStopwatchTicksToTimespanTicks(in TickInt stopwatchTicks) =>
            (long) (( (TickInt) stopwatchTicks* MonotonicTimeStamp<MonotonicStampContext>.TheToTsTickConversionFactorNumerator) /
                MonotonicTimeStamp<MonotonicStampContext>.ToToTsTickConversionFactorDenominator);

        private static Duration Interval(double value, double scale)
        {
            if (double.IsNaN(value))
                throw new ArgumentException("Parameter is NaN.", nameof(value));
            double ticks = value * scale;
            return IntervalFromDoubleTicks(ticks);
        }

        private static Duration IntervalFromDoubleTicks(double ticks)
        {
            if ((ticks > (double) TickInt.MaxValue) || (ticks < (double) TickInt.MinValue) || double.IsNaN(ticks))
                throw new OverflowException("Value cannot fit in a TimeSpan.");
            if (ticks >= long.MaxValue)
                return MaxValue;
            return new Duration((long)ticks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("ReSharper", "RedundantCast")]
        internal static TickInt TimeToTicks(int hour, int minute, int second)
        {
            // totalSeconds is bounded by 2^31 * 2^12 + 2^31 * 2^8 + 2^31,
            // which is less than 2^44, meaning we won't overflow totalSeconds.
            TickInt totalSeconds = (TickInt)hour * 3_600 + (TickInt)minute * 60 + (TickInt)second;
            if (totalSeconds > MaxSeconds || totalSeconds < MinSeconds)
                throw new ArgumentException("One or more of the values was too long to fit in a Duration");
            return totalSeconds * TicksPerSecond;
        }
        #endregion

        #region Internal methods used for unit testing

        internal static bool AreValuesCloseEnough(in Duration d, TimeSpan t)
        {
            if (ConvertStopwatchTicksToTimespanTicks(d._ticks) == t.Ticks) return true;
            long wholeMillisecondsDuration = Convert.ToInt64(Math.Truncate(d.TotalMilliseconds));
            long wholeMillisecondsTimespan = Convert.ToInt64(Math.Truncate(t.TotalMilliseconds));
            return wholeMillisecondsDuration == wholeMillisecondsTimespan;
        }

        internal static bool AreValuesCloseEnough(in Duration d, in PortableDuration pd)
        {
            if (PortableDuration.ConvertDurationTicksToPortableDurationTicks(in d._ticks) == pd._ticks) return true;
            long wholeMillisecondsDuration = Convert.ToInt64(Math.Truncate(d.TotalMilliseconds));
            long wholeMillisecondsPortableDuration = Convert.ToInt64(Math.Truncate(pd.TotalMilliseconds));
            return wholeMillisecondsDuration == wholeMillisecondsPortableDuration;
        }

        internal static bool AreValuesCloseEnough(in PortableDuration pd, TimeSpan t)
        {
            if (PortableDuration.ConvertPortableDurationTicksToTimespanTicks(pd._ticks) == t.Ticks) return true;
            long wholeMillisecondsPortableDuration = Convert.ToInt64(Math.Truncate(pd.TotalMilliseconds));
            long wholeMillisecondsTimeSpan = Convert.ToInt64(Math.Truncate(t.TotalMilliseconds));
            return wholeMillisecondsPortableDuration == wholeMillisecondsTimeSpan;
        }
        

        #endregion

        #region Private / Internal data
        /// <summary>
        /// Internal to allow fast direct access by other in this library.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")] //only internal by special dispensation
        internal readonly TickInt _ticks;
        #endregion
    }

    /// <summary>
    /// Exceptions related to stopwatches this library does not support are derived herefrom.
    /// </summary>
    public abstract class UnsupportedStopwatchException : ApplicationException
    {
        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="message">A non-null message for the user.</param>
        /// <param name="inner">An inner exception, if applicable or null otherwise.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> was null.</exception>
        protected UnsupportedStopwatchException([NotNull] string message, [CanBeNull] Exception inner) 
            : base(message ?? throw new ArgumentNullException(nameof(message)), inner) {}
    }
}
