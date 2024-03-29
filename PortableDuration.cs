﻿//Original version (before CJM Screws LLC made modifications)
//taken from https://raw.githubusercontent.com/dotnet/runtime/6072e4d3a7a2a1493f514cdf4be75a3d56580e84/src/libraries/System.Private.CoreLib/src/System/TimeSpan.cs
//that was licensed to the .NET Foundation by its original author.  In turn, the .NET Foundation licensed this code to CJM Screws, LLC under the MIT 
//license.  CJM Screws LLC licenses the version as modified to you under the MIT license.  CJM Screws LLC claims copyright to the modifications made 
//to this class, but makes no claim to the unaltered original.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using TickInt = HpTimeStamps.BigMath.Int128;
using PdInt = HpTimeStamps.BigMath.Int128;
using System.Text;

namespace HpTimeStamps
{
    /// <summary>
    /// Based on <see cref="TimeSpan"/> and <see cref="Duration"/>except <see cref="TicksPerSecond"/> is always a Nanoseconds frequency ... i.e.
    /// 1_000_000_000 rather than <see cref="TimeSpan"/>'s which is variable based on .NET implementation or <see cref="Duration"/> which is tied to <see cref="Stopwatch.Frequency"/>.
    /// </summary>
    /// <remarks>Since this type is always represented as nanoseconds regardless of environment it, unlike <see cref="Duration"/> is suitable for use across process boundaries and/or
    /// for serialization.</remarks>
    [DataContract]
    public readonly struct PortableDuration : IComparable<PortableDuration>, IEquatable<PortableDuration>
    {
        #region Readonly Public Static Values
        /// <summary>
        /// True if conversion to and from <see cref="Duration"/> is easy and not
        /// likely to have significant rounding errors.
        /// </summary>
        public static readonly bool EasyConversionToAndFromDuration;
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
        public static readonly PortableDuration Zero = new PortableDuration(0);

        /// <summary>
        /// Maximum value of a duration
        /// </summary>
        public static readonly PortableDuration MaxValue = new PortableDuration(PdInt.MaxValue);

        /// <summary>
        /// Minimum value of a duration
        /// </summary>
        public static readonly PortableDuration MinValue = new PortableDuration(PdInt.MinValue);

        internal static readonly PdInt TicksPerMicrosecond;
        internal static readonly PdInt TicksPerNanosecond;
        internal static readonly PdInt TicksPerSecondInternal;
        #endregion

        #region Readonly internal static values

        

        /// <summary>
        /// Longest positive period representable in seconds
        /// </summary>
        internal static readonly PdInt MaxSeconds;

        /// <summary>
        /// Longest negative period representable in seconds
        /// </summary>
        internal static readonly PdInt MinSeconds;

        /// <summary>
        /// Longest positive period represented in milliseconds
        /// </summary>
        internal static readonly PdInt MaxMilliseconds;

        /// <summary>
        /// Longest negative period represented in millisecond
        /// </summary>
        internal static readonly PdInt MinMilliseconds;

        /// <summary>
        /// Number of ticks in a tenth of a second
        /// </summary>
        internal static readonly PdInt TicksPerTenthSecond;

        /// <summary>
        /// Amount to shift a <see cref="PdInt"/> right to get its sign bit.
        /// </summary>
        internal const int PdIntRightShiftGetSignBitAmount = 127;

        #endregion

        #region Parsing Methods
        /// <summary>
        /// Parse a stringified portable duration back to a portable duration
        /// </summary>
        /// <param name="text">the stringified portable duration</param>
        /// <returns><paramref name="text"/>, deserialized into a <see cref="PortableDuration"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> was null.</exception>
        /// <exception cref="ArgumentException"><paramref name="text"/> could not be parsed into a <see cref="PortableDuration"/>.</exception>
        public static PortableDuration Parse(string text) =>
            Parse((text ?? throw new ArgumentNullException(nameof(text))).AsSpan());

        /// <summary>
        /// Parse a stringified portable duration back to a portable duration
        /// </summary>
        /// <param name="text">the stringified portable duration</param>
        /// <returns><paramref name="text"/>, deserialized into a <see cref="PortableDuration"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="text"/> could not be parsed into a <see cref="PortableDuration"/>.</exception>
        public static PortableDuration Parse(ReadOnlySpan<char> text)
        {
            text = text.Trim();
            text = text.UntilFirstOccurenceOf(' ');
            string noCommas = StripCommas(text);

            TickInt value = TickInt.Parse(noCommas.AsSpan());
            return new(value);
        }

        private static string StripCommas(ReadOnlySpan<char> text)
        {
            bool commas = HasCommas(in text);
            if (!commas)
            {
                return text.ToString();
            }
            StringBuilder sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c != ',')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();

            static bool HasCommas(in ReadOnlySpan<char> t)
            {
                for (int i = 0; i < t.Length; ++i)
                {
                    if (t[i] == ',')
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Try to parse a stringified portable duration back to a portable duration.
        /// </summary>
        /// <param name="text">the stringified portable duration</param>
        /// <returns>A portable duration with the value denoted by <paramref name="text"/> on success.
        /// Otherwise, <see langword="null"/>.</returns>
        public static PortableDuration? TryParse(string text) =>
            !string.IsNullOrWhiteSpace(text) ? TryParse(text.AsSpan()) : null;
        /// <summary>
        /// Try to parse a stringified portable duration back to a portable duration.
        /// </summary>
        /// <param name="text">the stringified portable duration</param>
        /// <returns>A portable duration with the value denoted by <paramref name="text"/> on success.
        /// Otherwise, <see langword="null"/>.</returns>
        public static PortableDuration? TryParse(ReadOnlySpan<char> text)
        {
            try
            {
                return !text.IsWhiteSpace() && !text.IsEmpty ? Parse(text) : null;
            }
            catch (Exception)
            {
                return null;
            }
        } 
        #endregion
        
        #region Conversion Operators

        /// <summary>
        /// Convert a timespan into a duration
        /// </summary>
        /// <param name="convertMe">value to convert</param>
        public static implicit operator PortableDuration(TimeSpan convertMe)
        {
            PdInt pdTicks = ConvertTimespanTicksToPortableDurationTicks(convertMe.Ticks);
            return new PortableDuration(in pdTicks);
        }

        /// <summary>
        /// Convert a duration to a portable duration
        /// </summary>
        /// <param name="d">The duration to convert to a portable duration</param>
        [SuppressMessage("ReSharper", "RedundantCast")]
        public static explicit operator PortableDuration(in Duration d) =>
            new PortableDuration(((PdInt) d._ticks) * ((PdInt)TicksPerSecond) / Duration.TicksPerSecond );

        /// <summary>
        /// Convert a portable duration into a duration
        /// </summary>
        /// <param name="d">the portable duration to convert</param>
        /// <returns>the portable duration</returns>
        /// <exception cref="OverflowException">Cannot fit in Duration/</exception>
        [SuppressMessage("ReSharper", "RedundantCast")]
        public static explicit operator Duration(in PortableDuration d) =>
            new Duration( ((TickInt) d._ticks) * ((TickInt)Duration.TicksPerSecond) / TicksPerSecond );

        /// <summary>
        /// Convert a duration into a timespan
        /// </summary>
        /// <param name="convertMe">value to convert</param>
        /// <exception cref="OverflowException">Can't fit.</exception>
        public static explicit operator TimeSpan(in PortableDuration convertMe)
        {
            long timeSpanTicks = ConvertPortableDurationTicksToTimespanTicks(convertMe._ticks);
            return TimeSpan.FromTicks(timeSpanTicks);
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Convert timespan ticks into a duration
        /// </summary>
        /// <param name="timespanTicks">the timespan ticks</param>
        /// <returns>a duration</returns>
        public static PortableDuration FromTimespanTicks(long timespanTicks)
        {
            PdInt swTicks = ConvertTimespanTicksToPortableDurationTicks(timespanTicks);
            return new PortableDuration(in swTicks);
        }

        /// <summary>
        /// Compute a duration from a value representing days
        /// </summary>
        /// <param name="value">Value representing days</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a PortableDuration.</exception>
        public static PortableDuration FromDays(double value) => Interval(value, TicksPerDay);

        /// <summary>
        /// Compute a duration from a value representing hours
        /// </summary>
        /// <param name="value">Value representing days</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a PortableDuration.</exception>
        public static PortableDuration FromHours(double value) => Interval(value, TicksPerHour);

        /// <summary>
        /// Compute a duration from a value representing milliseconds
        /// </summary>
        /// <param name="value">Value representing milliseconds</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a PortableDuration.</exception>
        public static PortableDuration FromMilliseconds(double value) => Interval(value, TicksPerMillisecond);

        /// <summary>
        /// Compute a duration from a value representing microseconds
        /// </summary>
        /// <param name="value">Value representing milliseconds</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a PortableDuration.</exception>
        public static PortableDuration FromMicroseconds(double value)
        {
            Debug.Assert(TicksPerSecond == 1_000_000_000, "Expect pd tics to always be nanoseconds");
            double result = value * 1_000.0;
            return new PortableDuration((PdInt) result);
        }
        
        /// <summary>
        /// Compute a duration from a value representing microseconds
        /// </summary>
        /// <param name="value">Value representing microseconds</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a PortableDuration.</exception>
        public static PortableDuration FromMicroseconds(long value)
        {
            Debug.Assert(TicksPerSecond == 1_000_000_000, "Expect pd tics to always be nanoseconds");
            PdInt result = ((PdInt) value) * 1_000L;
            return new PortableDuration(in result);
        }
        /// <summary>
        /// Compute a duration from a value representing nanoseconds
        /// </summary>
        /// <param name="nanoseconds">Value representing nanoseconds</param>
        /// <returns>A duration</returns>
        public static PortableDuration FromNanoseconds(long nanoseconds) => new PortableDuration(nanoseconds);

        /// <summary>
        /// Compute a duration from a value representing minutes
        /// </summary>
        /// <param name="value">Value representing minutes</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a PortableDuration.</exception>
        public static PortableDuration FromMinutes(double value) => Interval(value, TicksPerMinute);

        /// <summary>
        /// Compute a duration from a value representing seconds
        /// </summary>
        /// <param name="value">Value representing seconds</param>
        /// <returns>A duration</returns>
        /// <exception cref="ArgumentException">Value not representable as a PortableDuration.</exception>
        public static PortableDuration FromSeconds(double value) => Interval(value, TicksPerSecond);

        /// <summary>
        /// Compute a duration from an integral value representing seconds.
        /// </summary>
        /// <param name="value">The value in seconds.</param>
        /// <returns>A portable duration of the same value as the number of seconds
        /// expressed by <paramref name="value"/>.</returns>
        public static PortableDuration FromSeconds(long value)
        {
            PdInt nanoSecondsFromSecondsConversionFactor = 1_000_000_000L;
            return new PortableDuration(value * nanoSecondsFromSecondsConversionFactor);
        }

        /// <summary>
        /// Create a duration from ticks
        /// </summary>
        /// <param name="value">ticks</param>
        /// <returns>the value</returns>
        internal static PortableDuration FromStopwatchTicks(in TickInt value)
        {
            TickInt converted = ConvertDurationTicksToPortableDurationTicks(in value);
            return new PortableDuration(converted );
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// portable duration ticks
        /// </summary>
        internal PdInt InternalTicks => _ticks;

        /// <summary>
        /// Number of whole days represented, fractional time remaining discarded.
        /// </summary>
        public int Days => (int) (_ticks / TicksPerDay);

        /// <summary>
        /// Number of whole hours represented, fractional time remaining discarded
        /// </summary>
        public int Hours => (int) ((_ticks / TicksPerHour) % 24);

        /// <summary>
        /// Number of whole milliseconds represented, fractional time remaining discarded
        /// </summary>
        public int Milliseconds => (int) ((_ticks / TicksPerMillisecond) % 1_000);

        /// <summary>
        /// Number of whole microseconds represented, fractional time remaining discarded
        /// </summary>
        public long Microseconds => (long) ((_ticks / TicksPerMicrosecond) % 1_000_000);

        /// <summary>
        /// Number of whole nanoseconds represented, fractional time remaining discarded
        /// </summary>
        /// <exception cref="OverflowException">Nanoseconds will not fit in <see cref="long"/>.</exception>
        public long Nanoseconds => (long) ((_ticks / TicksPerNanosecond) % 1_000_000_000);

        /// <summary>
        /// Number of whole minutes represented, fractional time remaining discarded
        /// </summary>
        public int Minutes => (int) ((_ticks / TicksPerMinute) % 60);

        /// <summary>
        /// Number of whole seconds represented, fractional time remaining discarded
        /// </summary>
        public int Seconds => (int) ((_ticks / TicksPerSecond) % 60);

        /// <summary>
        /// The duration represented in days, including fractional parts
        /// </summary>
        public double TotalDays => ((double) _ticks) / TicksPerDay;

        /// <summary>
        /// The duration represented in hours, including fractional parts
        /// </summary>
        public double TotalHours => (double) _ticks / TicksPerHour;

        /// <summary>
        /// PortableDuration represented in microseconds, including fractional parts
        /// </summary>
        public double TotalMicroseconds => (double) _ticks / (double) TicksPerMicrosecond;

        /// <summary>
        /// Get ticks whole ticks as timespan ticks (1/10th of microsecond)
        /// if value will fit in 64 bit signed integer. If not, TenthsOfMicroseconds
        /// will be null.  Also get nano-seconds remainder (0-99)
        /// </summary>
        public (long? TenthsOfMicroseconds, int NanosecondRemainder) Ticks
        {
            get
            {
                long? tsTicks=null;
                (PdInt tenthsOfMicrosecond, PdInt nanoRem) = PdInt.DivRem(in _ticks, 100);
                if (tenthsOfMicrosecond < long.MinValue || tenthsOfMicrosecond > long.MaxValue)
                {
                    tsTicks = (long) tenthsOfMicrosecond;
                }

                return (tsTicks, (int) nanoRem);
            }
        }
        
        /// <summary>
        /// PortableDuration represented in nanoseconds, including fractional parts
        /// </summary>
        public double TotalNanoseconds => (double) _ticks / (double) TicksPerNanosecond;

        
        /// <summary>
        /// The duration represented in milliseconds, including fractional parts
        /// </summary>
        [SuppressMessage("ReSharper", "RedundantCast")]
        public double TotalMilliseconds
        {
            get
            {
                double temp = (double) _ticks / TicksPerMillisecond;
                if (temp > (double) MaxMilliseconds)
                    return (double) MaxMilliseconds;

                if (temp < (double) MinMilliseconds)
                    return (double) MinMilliseconds;

                return temp;
            }
        }
        
        /// <summary>
        /// The duration represented in minutes, including fractional parts
        /// </summary>
        public double TotalMinutes => (double) _ticks / TicksPerMinute;

        /// <summary>
        /// The duration represented in seconds, including fractional parts
        /// </summary>
        public double TotalSeconds => (double) _ticks / TicksPerSecond;
        
        #endregion

        #region CTORS

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="nanoseconds"># of nanoseconds</param>
        internal PortableDuration(in PdInt nanoseconds) => _ticks = nanoseconds;

        /// <summary>
        /// Create a duration from stopwatch ticks
        /// </summary>
        /// <param name="nanoseconds">number of nanoseconds</param>
        public PortableDuration(long nanoseconds) => _ticks = nanoseconds;

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="hours">Hours</param>
        /// <param name="minutes">Minutes</param>
        /// <param name="seconds">Seconds</param>
        /// <exception cref="ArgumentException">Period too long to fit.</exception>
        public PortableDuration(int hours, int minutes, int seconds) => _ticks = TimeToTicks(hours, minutes, seconds);

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="days">days</param>
        /// <param name="hours">hours</param>
        /// <param name="minutes">minutes</param>
        /// <param name="seconds">seconds</param>
        /// <exception cref="ArgumentException">Period too long to fit</exception>
        public PortableDuration(int days, int hours, int minutes, int seconds)
            : this(days, hours, minutes, seconds, 0)
        {
        }

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
        public PortableDuration(int days, int hours, int minutes, int seconds, int milliseconds)
        {
            long totalMilliSeconds =
                ((long) days * 3_600 * 24 + (long) hours * 3_600 + (long) minutes * 60 + seconds) * 1_000 +
                milliseconds;
            if (totalMilliSeconds > MaxMilliseconds || totalMilliSeconds < MinMilliseconds)
                throw new ArgumentException("Period specified will not fit in a timespan.");
            _ticks = (TickInt) totalMilliSeconds * TicksPerMillisecond;
        }

        static PortableDuration()
        {
            TicksPerSecond = 1_000_000_000;
            TicksPerMillisecond = TicksPerSecond / 1_000;
            TicksPerMicrosecond = (TickInt) TicksPerMillisecond / 1_000;
            TicksPerNanosecond = 1;
            TicksPerMinute = TicksPerSecond * 60;
            TicksPerHour = TicksPerMinute * 60;
            TicksPerDay = TicksPerHour * 24;
            MaxSeconds = PdInt.MaxValue / TicksPerSecond;
            MinSeconds = PdInt.MinValue / TicksPerSecond;
            MaxMilliseconds = PdInt.MaxValue / TicksPerMillisecond;
            MinMilliseconds = PdInt.MinValue / TicksPerMillisecond;
            TicksPerTenthSecond = TicksPerMillisecond * 100;
            TicksPerNanosecond = 1;
            TicksPerMicrosecond = 1_000;
            TicksPerSecondInternal = TicksPerNanosecond * TicksPerSecond;

            long quotient = Math.DivRem(TicksPerSecond, Duration.TicksPerSecond, out long remainder);
            EasyConversionToAndFromDuration = remainder == 0 && (quotient == 1 || (quotient % 10 == 0));

            Debug.Assert(TicksPerSecondInternal == TicksPerSecond);
        }

        #endregion

        #region ToString Methods

        /// <summary>
        /// Get a string representation (nanoseconds)
        /// </summary>
        /// <returns>a string representation</returns>
        public override string ToString() => _ticks.ToString("N") + " nanoseconds";
        #endregion

        #region Equatable and Comparable Methods and Operators
        /// <summary>
        /// Test two durations for equality
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if equal false otherwise</returns>
        public static bool operator ==(in PortableDuration t1, in PortableDuration t2) => t1._ticks == t2._ticks;
        /// <summary>
        /// Test two durations for inequality
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if not equal false otherwise</returns>
        public static bool operator !=(in PortableDuration t1, in PortableDuration t2) => t1._ticks != t2._ticks;
        /// <summary>
        /// Test two durations for to see if <paramref name="t1"/> is less than <paramref name="t2"/>
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if <paramref name="t1"/> is less than <paramref name="t2"/>, false otherwise</returns>
        public static bool operator <(in PortableDuration t1, in PortableDuration t2) => t1._ticks < t2._ticks;
        /// <summary>
        /// Test two durations for to see if <paramref name="t1"/> is less than or equal to <paramref name="t2"/>
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if <paramref name="t1"/> is less than or equal to <paramref name="t2"/>, false otherwise</returns>
        public static bool operator <=(in PortableDuration t1, in PortableDuration t2) => t1._ticks <= t2._ticks;
        /// <summary>
        /// Test two durations for to see if <paramref name="t1"/> is greater than <paramref name="t2"/>
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if <paramref name="t1"/> is greater than <paramref name="t2"/>, false otherwise</returns>
        public static bool operator >(in PortableDuration t1, in PortableDuration t2) => t1._ticks > t2._ticks;

        /// <summary>
        /// Test two durations for to see if <paramref name="t1"/> is greater than or equal to <paramref name="t2"/>
        /// </summary>
        /// <param name="t1">left hand operand</param>
        /// <param name="t2">right hand operand</param>
        /// <returns>true if <paramref name="t1"/> is greater than or equal to <paramref name="t2"/>, false otherwise</returns>
        public static bool operator >=(in PortableDuration t1, in PortableDuration t2) => t1._ticks >= t2._ticks;
        /// <inheritdoc />
        public override bool Equals(object value) => value is PortableDuration d && d == this;
        /// <inheritdoc />
        public bool Equals(PortableDuration obj) => obj == this;
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
        public int CompareTo(PortableDuration otherValue) => Compare(in this, in otherValue);
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
        public static int Compare(in PortableDuration lhs, in PortableDuration rhs)
        {
            if (lhs._ticks > rhs._ticks) return 1;
            if (lhs._ticks < rhs._ticks) return -1;
            return 0;
        }
        #endregion

        #region Mathematical Methods and Operators

        /// <summary>
        /// For a given portable duration query the total whole seconds and the nanoseconds remainder.
        ///  </summary>
        /// <returns>Total whole seconds (can be negative), also nanoseconds remainder.  Remainder always positive if
        /// TotalWholeSeconds non-zero.</returns>
        public (long TotalWholeSeconds, long RemainderNanoseconds) GetTotalWholeSecondsAndRemainder()
        {
            const long nanoSecsPerSec = 1_000_000_000L;
            long wholeSeconds, remainderNanoseconds;
            (PdInt tempQuotient, PdInt tempRemainder) = PdInt.DivRem(in _ticks, TicksPerSecond);
            if (tempRemainder < 0 && tempQuotient < 0)
            {
                tempRemainder = nanoSecsPerSec - -tempRemainder;
            }
            checked
            {
                wholeSeconds = (long)tempQuotient;
                remainderNanoseconds = (long)tempRemainder;
            }
            Debug.Assert(wholeSeconds == 0 || remainderNanoseconds >= 0);
            Debug.Assert(wholeSeconds != 0 || remainderNanoseconds < nanoSecsPerSec);
            Debug.Assert((wholeSeconds != 0 && remainderNanoseconds < nanoSecsPerSec) ||
                         remainderNanoseconds > -nanoSecsPerSec);
            return (wholeSeconds, remainderNanoseconds);
        }


        /// <summary>
        /// Attempt to get the total nanoseconds as a 64 bit int
        /// without any loss of precision.
        /// </summary>
        /// <returns>The total value of the duration expressed in nanoseconds
        /// if value fits in <see cref="Int64"/>, null otherwise.</returns>
        [Pure]
        public long? TryGetTotalNanoseconds() => _ticks <= long.MaxValue ? (long)_ticks : null;

        /// <summary>
        /// Multiply this value by a specified factor
        /// </summary>
        /// <param name="factor">the factor</param>
        /// <returns>the product</returns>
        [Pure]
        public PortableDuration Multiply(double factor) => this * factor;
        /// <summary>
        /// Divide this value by a specified divisor
        /// </summary>
        /// <param name="divisor">the divisor</param>
        /// <returns>the product</returns>
        [Pure]
        public PortableDuration Divide(double divisor) => this / divisor;
        /// <summary>
        /// Divide this value by another duration
        /// </summary>
        /// <param name="ts">the divisor</param>
        /// <returns>the quotient</returns>
        [Pure]
        public double Divide(PortableDuration ts) => this / ts;
        /// <summary>
        /// Subtract two durations
        /// </summary>
        /// <param name="t1">minuend</param>
        /// <param name="t2">subtrahend</param>
        /// <returns>difference</returns>
        /// <exception cref="OverflowException">Operation overflow</exception>
        public static PortableDuration operator -(in PortableDuration t1, in PortableDuration t2) => t1.Subtract(in t2);
        /// <summary>
        /// Apply unary + operator
        /// </summary>
        /// <param name="t">operand</param>
        /// <returns>a value equal to <paramref name="t"/>/</returns>
        public static PortableDuration operator +(in PortableDuration t) => t;
        /// <summary>
        /// Add two addends
        /// </summary>
        /// <param name="t1">first addend</param>
        /// <param name="t2">second addend</param>
        /// <returns>sum</returns>
        /// <exception cref="OverflowException">Operation resulted in overflow</exception>
        public static PortableDuration operator +(in PortableDuration t1, in PortableDuration t2) => t1.Add(t2);
        /// <summary>
        /// Multiply a duration by a factor
        /// </summary>
        /// <param name="factor">the factor</param>
        /// <param name="timeSpan">the duration</param>
        /// <returns>the product</returns>
        /// <exception cref="OverflowException">Operation resulted in overflow.</exception>
        public static PortableDuration operator *(double factor, in PortableDuration timeSpan) => timeSpan * factor;
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
        public static double operator /(in PortableDuration t1, in PortableDuration t2) => (double)t1.InternalTicks / (double)t2.InternalTicks;

        /// <summary>
        /// Multiply a duration 
        /// </summary>
        /// <param name="timeSpan">PortableDuration factor</param>
        /// <param name="factor">factor</param>
        /// <returns>product</returns>
        /// <exception cref="ArgumentException"><paramref name="factor"/> was nan</exception>
        /// <exception cref="OverflowException">operation resulted in overflow</exception>
        public static PortableDuration operator *(in PortableDuration timeSpan, double factor)
        {
            if (double.IsNaN(factor))
            {
                throw new ArgumentException("NaN is not a valid value for parameter.", nameof(factor));
            }

            // Rounding to the nearest tick is as close to the result we would have with unlimited
            // precision as possible, and so likely to have the least potential to surprise.
            double ticks = Math.Round((double)timeSpan.InternalTicks * factor);
            return IntervalFromDoubleTicks(ticks);
        }

        /// <summary>
        /// Get the absolute value of this PortableDuration
        /// </summary>
        /// <returns>the absolute value</returns>
        public PortableDuration AbsoluteValue()
        {
            if (InternalTicks == MinValue.InternalTicks)
                throw new OverflowException("This value is the most negative value and has no positive 2's complement counterpart.");
            return new PortableDuration(_ticks >= 0 ? _ticks : -_ticks);
        }

        /// <summary>
        /// Return the additive inverse of this value
        /// </summary>
        /// <returns>The additive inverse</returns>
        [Pure]
        public PortableDuration Negate()
        {
            if (InternalTicks == MinValue.InternalTicks)
                throw new OverflowException("This value is the most negative value " +
                                            "possible and has no positive counterpart in a 2's complement representation.");
            return new PortableDuration(-_ticks);
        }

        /// <summary>
        /// Add another value to this one
        /// </summary>
        /// <param name="ts">value to add to this one</param>
        /// <returns>sum</returns>
        /// <exception cref="OverflowException">addition caused overflow</exception>
        [Pure]
        public PortableDuration Add(in PortableDuration ts)
        {
            TickInt result = _ticks + ts._ticks;
            // Overflow if signs of operands was identical and result's
            // sign was opposite.
            // >> TickIntRightShiftGetSignBitAmount gives the sign bit (either 64 1's or 64 0's).
            if ((_ticks >> PdIntRightShiftGetSignBitAmount == ts._ticks >> PdIntRightShiftGetSignBitAmount) &&
                (_ticks >> PdIntRightShiftGetSignBitAmount != result >> PdIntRightShiftGetSignBitAmount))
                throw new OverflowException("The addition resulted in overflow.");
            return new PortableDuration(result);
        }


        /// <summary>
        /// Subtract another value from this one
        /// </summary>
        /// <param name="ts">the subtrahend</param>
        /// <returns>the difference</returns>
        /// <exception cref="OverflowException">Result caused overflow.</exception>
        [Pure]
        public PortableDuration Subtract(in PortableDuration ts)
        {
            TickInt result = _ticks - ts._ticks;
            // Overflow if signs of operands was different and result's
            // sign was opposite from the first argument's sign.
            // >> TickIntRightShiftGetSignBitAmount gives the sign bit 
            if ((_ticks >> PdIntRightShiftGetSignBitAmount != ts._ticks >>
                    PdIntRightShiftGetSignBitAmount) &&
                (_ticks >> PdIntRightShiftGetSignBitAmount != result
                    >> PdIntRightShiftGetSignBitAmount))
                throw new OverflowException();
            return new PortableDuration(result);
        }

        /// <summary>
        /// Negate the duration
        /// </summary>
        /// <param name="t">The duration to negate</param>
        /// <returns>the additive inverse</returns>
        public static PortableDuration operator -(in PortableDuration t)
        {
            if (t._ticks == MinValue._ticks)
                throw new OverflowException("The duration supplied has no positive 2's complement counterpart.");
            return new PortableDuration(-t._ticks);
        }

        /// <summary>
        /// Divide a duration by a divisor
        /// </summary>
        /// <param name="timeSpan">the dividend</param>
        /// <param name="divisor">divisor</param>
        /// <returns>quotient</returns>
        /// <exception cref="ArgumentException"><paramref name="divisor"/> was <see cref="double.NaN"/>.</exception>
        public static PortableDuration operator /(in PortableDuration timeSpan, double divisor)
        {
            if (double.IsNaN(divisor))
            {
                throw new ArgumentException("NaN is not a valid value for parameter", nameof(divisor));
            }

            double ticks = Math.Round((double)timeSpan.InternalTicks / divisor);
            return IntervalFromDoubleTicks(ticks);
        }
        #endregion

        #region Private and Internal Helper Methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ConvertStopwatchTicksToTimespanTicks(long stopwatchTicks) =>
            (stopwatchTicks * MonotonicTimeStamp<MonotonicStampContext>.TheToTsTickConversionFactorNumerator) /
            MonotonicTimeStamp<MonotonicStampContext>.ToToTsTickConversionFactorDenominator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static PdInt ConvertTimespanTicksToPortableDurationTicks(long timespanTicks) =>
            timespanTicks * TicksPerSecondInternal / TimeSpan.TicksPerSecond;
        internal static long ConvertPortableDurationTicksToTimespanTicks(in PdInt pdTicks) =>
            (long) (pdTicks * TimeSpan.TicksPerSecond / TicksPerSecondInternal);
        internal static TickInt ConvertPortableDurationTicksToDurationTicks(in PdInt portableDurationTicks) =>
            portableDurationTicks * Duration.TicksPerSecond / TicksPerSecond;

        internal static TickInt ConvertDurationTicksToPortableDurationTicks(in TickInt durationTicks) =>
            durationTicks * TicksPerSecond / Duration.TicksPerSecond;
        
        private static PortableDuration Interval(double value, double scale)
        {
            if (double.IsNaN(value))
                throw new ArgumentException("Parameter is NaN.", nameof(value));
            double ticks = value * scale;
            return IntervalFromDoubleTicks(ticks);
        }

        private static PortableDuration IntervalFromDoubleTicks(double ticks) 
        {
            //bug 19 fix: remove useless comparison to long max and doubly useless forgetting to check for long min.  
            //the rep here is in int128 nanoseconds ticks.  doesn't matter for portable duration purposes if 
            //cannot fit in long.

            if ((ticks > (double)PdInt.MaxValue) || (ticks < (double)PdInt.MinValue) || double.IsNaN(ticks))
                throw new OverflowException("Value cannot fit in a PortableDuration.");
            
            return new PortableDuration((PdInt) ticks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("ReSharper", "RedundantCast")]
        internal static TickInt TimeToTicks(int hour, int minute, int second)
        {
            // totalSeconds is bounded by 2^31 * 2^12 + 2^31 * 2^8 + 2^31,
            // which is less than 2^44, meaning we won't overflow totalSeconds.
            TickInt totalSeconds = (PdInt)hour * 3_600 + (PdInt)minute * 60 + (PdInt)second;
            if (totalSeconds > MaxSeconds || totalSeconds < MinSeconds)
                throw new ArgumentException("One or more of the values was too long to fit in a PortableDuration");
            return totalSeconds * TicksPerSecond;
        }
        #endregion

        #region Private / Internal data
        /// <summary>
        /// Internal to allow fast direct access by other in this library.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")] //only internal by special dispensation
        [DataMember] internal readonly PdInt _ticks;
        #endregion

       
    }

    internal static class CharSpanExtensions
    {
        public static ReadOnlySpan<char> UntilFirstOccurenceOf(this in ReadOnlySpan<char> text, char lookForMe)
        {
            int firstIdx = text.FirstIndexOf(lookForMe);
            Debug.Assert(firstIdx < text.Length);
            return firstIdx switch
            {
                < 0 => text,
                0 => Span<char>.Empty,
                _ => text.Slice(0, firstIdx)
            };
        }

        public static int FirstIndexOf(this in ReadOnlySpan<char> text, char findMe)
        {
            if (text.IsEmpty)
                return -1;
            for (int i = 0; i < text.Length; ++i)
            {
                if (findMe == text[i])
                    return i;
            }
            return -1;
        }

        public static Span<char> UntilFirstOccurenceOf(this in Span<char> text, char lookForMe)
        {
            int firstIdx = text.FirstIndexOf(lookForMe);
            Debug.Assert(firstIdx < text.Length);
            return firstIdx switch
            {
                < 0 => text,
                0 => Span<char>.Empty,
                _ => text.Slice(0, firstIdx)
            };
        }

        public static int FirstIndexOf(this in Span<char> text, char findMe)
        {
            if (text.IsEmpty)
                return -1;
            for (int i = 0; i < text.Length; ++i)
            {
                if (findMe == text[i])
                    return i;
            }
            return -1;
        }
    }
}
