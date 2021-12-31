using System;

namespace HpTimeStamps
{
    /// <summary>
    /// A time stamp formatted to be easily convertible to and from a protobuf format
    /// </summary>
    public readonly struct ProtobufFormatStamp : IEquatable<ProtobufFormatStamp>, IComparable<ProtobufFormatStamp>
    {
        /// <summary>
        /// Implicitly convert a <see cref="ValueTuple{T1,T2}"/> whose <see cref="ValueTuple{T1,T2}.Item1"/> is of type <see cref="long"/> and
        /// represents whole seconds since unix epoch and whose <see cref="ValueTuple{T1,T2}.Item2"/> is of type <see cref="int"/> and represents
        /// fractional seconds, nanoseconds resolution.
        /// into a <see cref="ProtobufFormatStamp"/>.  No validation performed. 
        /// </summary>
        /// <param name="convertMe">value to convert. </param>
        /// <returns>Equivalent <see cref="ProtobufFormatStamp"/> given value of <paramref name="convertMe"/>.</returns>
        public static implicit operator ProtobufFormatStamp(ValueTuple<long, int> convertMe) =>
            new(convertMe.Item1, convertMe.Item2);

        /// <summary>
        /// Convert a portable monotonic timestamp into a format compatible with
        /// google's protobuf timestamp.
        /// </summary>
        /// <param name="stamp"></param>
        /// <returns>An easy-to-convert-to-protobuf representation of <paramref name="stamp"/>.</returns>
        public static explicit operator ProtobufFormatStamp(in PortableMonotonicStamp stamp)
        {
            (long wholeSeconds, long remainder) =
                (stamp - PortableMonotonicStamp.UnixEpochStamp).GetTotalWholeSecondsAndRemainder();
            (wholeSeconds, remainder) = (wholeSeconds, remainder) switch
            {
                (var w, var f) when w != 0 && f < 0 => throw new InvalidProtobufStampException(wholeSeconds,
                    (int)remainder, $"Invalid Protobuf stamp value -- whole secs: {wholeSeconds:N0}; nanos: {remainder:N0}"),
                (0, var frac) => (0, frac),
                (var whole, 0L) => (whole, 0L),
                (> 0, var frac) => (wholeSeconds, frac),
                (< 0, var frac) => (wholeSeconds - 1, frac),
            };
            return new ProtobufFormatStamp(wholeSeconds, (int)remainder);
        }

        /// <summary>
        /// Convert a valid protobuf format stamp into a portable monotonic stamp.
        /// </summary>
        /// <param name="myStamp">value to convert</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidProtobufStampException">The protobuf stamp is not a valid value.</exception>
        public static explicit operator PortableMonotonicStamp(in ProtobufFormatStamp myStamp)
        {
            myStamp.Validate();
            PortableDuration wholeSeconds = PortableDuration.FromSeconds(myStamp._seconds);
            return PortableMonotonicStamp.UnixEpochStamp + wholeSeconds + (PortableDuration.Compare(in wholeSeconds, in PortableDuration.Zero), myStamp._nanos) switch
            {
                (0, var nano) => PortableDuration.FromNanoseconds(nano),
                (< 0, var nano and >= 0) => PortableDuration.FromNanoseconds(nano),
                (> 0, var nano and >= 0) => PortableDuration.FromNanoseconds(nano),
                (> 0 or <0, var nano and < 0) => throw new InvalidProtobufStampException(
                    (long) Math.Truncate(wholeSeconds.TotalSeconds), nano, 
                    $"Illegally formatted stamp -- nano (value: {nano}) is negative and so " +
                    $"is whole seconds (value: {(long)Math.Truncate(wholeSeconds.TotalSeconds)})."),
            };
        }

        /// <summary>
        /// How many whole seconds since unix epoch (negative indicates seconds preceding epoch)
        /// </summary>
        public long Seconds => _seconds;

        /// <summary>
        /// How many fractional seconds since unix epoch.  Valid values: 0 - 999_999_999 (inclusive)
        /// </summary>
        public int Nanoseconds => _nanos;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nanos"></param>
        /// <param name="seconds"></param>
        public ProtobufFormatStamp(long seconds, int nanos) 
            => (_nanos, _seconds) = (nanos, seconds);

        /// <summary>
        /// Compare to protobuf formatted stamps to establish an ordering between them
        /// </summary>
        /// <param name="lhs">left hand operand</param>
        /// <param name="rhs">right hand operand</param>
        /// <returns>A negative number if <paramref name="lhs"/> precedes <paramref name="rhs"/> in their ordering;
        /// Zero if <paramref name="lhs"/> occupies the same position as <paramref name="rhs"/> in their ordering; and
        /// A positive number if <paramref name="lhs"/> succeeds <paramref name="rhs"/> in their ordering
        /// </returns>
        /// <exception cref="InvalidProtobufStampException"><paramref name="lhs"/> or <paramref name="rhs"/> (or both) are not
        /// valid protbuf timestamp values and thus cannot be meaningfully compared.</exception>
        public static int Compare(in ProtobufFormatStamp lhs, in ProtobufFormatStamp rhs)
        {
            lhs.Validate();
            rhs.Validate();
            int ret;
            int secondsComparison = lhs._seconds.CompareTo(rhs._seconds);
            if (secondsComparison == 0)
            {
                bool negateLNanos = lhs._seconds < 0;
                bool negateRNanos = rhs._seconds < 0;

                int lNanos = negateLNanos ? -lhs._nanos : lhs._nanos;
                int rNanos = negateRNanos ? -rhs._nanos : rhs._nanos;
                ret = lNanos.CompareTo(rNanos);
            }
            else
            {
                ret = secondsComparison;
            }

            return ret;
        }

        /// <summary>
        /// Calculates value-based hashcode
        /// </summary>
        /// <returns>A hash code</returns>
        public override int GetHashCode()
        {
            int hash = _nanos;
            unchecked
            {
                hash = (hash * 397) ^ _seconds.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Query to see whether <paramref name="lhs"/> has the same value as <paramref name="rhs"/>/
        /// </summary>
        /// <param name="lhs">The left comparand</param>
        /// <param name="rhs">The right comparand</param>
        /// <returns>True only if <paramref name="lhs"/> and <paramref name="rhs"/> have the same value.</returns>
        public static bool operator ==(in ProtobufFormatStamp lhs, in ProtobufFormatStamp rhs) =>
            lhs._seconds == rhs._seconds && lhs._nanos == rhs._nanos;
        /// <summary>
        /// Query to see whether <paramref name="lhs"/> has a distinct value from <paramref name="rhs"/>.
        /// </summary>
        /// <param name="lhs">The left comparand</param>
        /// <param name="rhs">The right comparand</param>
        /// <returns>True only if <paramref name="lhs"/> and <paramref name="rhs"/> have distinct values.</returns>
        public static bool operator !=(in ProtobufFormatStamp lhs, in ProtobufFormatStamp rhs) =>
            !(lhs == rhs);

        /// <inheritdoc />
        public bool Equals(ProtobufFormatStamp other) => other == this;

        /// <summary>
        /// Compare the value of this stamp with that of another to establish an ordering.
        /// </summary>
        /// <param name="other">The other value</param>
        /// <returns>A negative number if this <see cref="ProtobufFormatStamp"/> value precedes <paramref name="other"/> in the ordering.
        /// Zero if this <see cref="ProtobufFormatStamp"/> value occupies the same place as <paramref name="other"/> in the ordering.
        /// A positive number if this <see cref="ProtobufFormatStamp"/> value succeeds <paramref name="other"/> in the ordering.</returns>
        /// <exception cref="InvalidProtobufStampException">Either this <see cref="ProtobufFormatStamp"/> value or <paramref name="other"/> (or both)
        /// are invalid timestamp values and cannot be meaningfully compared.</exception>
        public int CompareTo(ProtobufFormatStamp other) => Compare(in this, in other);

        /// <inheritdoc />
        public override bool Equals(object other) => other is ProtobufFormatStamp pbfs && pbfs == this;

        /// <summary>
        /// Check whether value is valid.
        /// </summary>
        /// <exception cref="InvalidProtobufStampException">The protobuf stamp is not a valid value.</exception>
        public void Validate() =>
            InvalidProtobufStampException.ThrowIf(Seconds, Nanoseconds);

        /// <summary>
        /// Used for deconstructing into component parts
        /// </summary>
        /// <param name="seconds">seconds since unix epoch</param>
        /// <param name="nanoseconds">fractional component of timestamp -- nanoseconds.</param>
        public void Deconstruct(out long seconds, out int nanoseconds) => (seconds, nanoseconds) = (_seconds, _nanos);


        /// <summary>
        /// Maximum per permissible value for the <see cref="Nanoseconds"/> property
        /// </summary>
        public const long MaxNanos = 999_999_999;
        private readonly int _nanos;
        private readonly long _seconds;
        private static readonly PortableMonotonicStamp TheUnixEpochStamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
