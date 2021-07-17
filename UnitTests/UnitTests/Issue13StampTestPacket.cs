using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using HpTimeStamps;
using JetBrains.Annotations;
using Xunit;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
namespace UnitTests
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicContext>;

    [DataContract]
    public readonly struct Issue13StampTestPacket : IEquatable<Issue13StampTestPacket>, IComparable<Issue13StampTestPacket>
    {
        public static ByRefRoList<Issue13StampTestPacket> CreateManyTestPackets(int numPackets)
        {
            if (numPackets < 0) throw new ArgumentOutOfRangeException(nameof(numPackets), numPackets, @"Argument may not be negative.");
            Issue13StampTestPacket[] arr = new Issue13StampTestPacket[numPackets];
            int count = 0;
            while (count < numPackets)
            {
                arr[count++] = CreateNewTestPacket();
            }
            return ByRefRoList<Issue13StampTestPacket>.WrapExistingArray(arr);
        }

        public static Issue13StampTestPacket CreateNewTestPacket()
        {

            DateTime acquiredWallTime = DateTime.Now;
            MonotonicStamp stamp = HpTimeStamps.MonotonicTimeStampUtil<MonotonicContext>.StampNow;
            ref readonly var context = ref stamp.Context;
            long systemTps = context.TicksPerSecond;
            long refTicks = context.ReferenceTicks;
            string stringifiedMonoStamp = stamp.ToString();
            DateTime utcDtFromStamp = stamp.ToUtcDateTime();
            PortableMonotonicStamp castFromMono = (PortableMonotonicStamp)stamp;
            PortableMonotonicStamp toStampedFromMono = stamp.ToPortableStamp();
            string castPortableString = castFromMono.ToString();
            string toPortableString = toStampedFromMono.ToString();

            return new Issue13StampTestPacket(refTicks, acquiredWallTime, systemTps, toPortableString,
                castPortableString, toStampedFromMono, castFromMono, utcDtFromStamp, stringifiedMonoStamp);
        }

        public long AcqSysRefTicks => _acqSysRefTicks;
        public DateTime AcquiredSystemWallClockDateTime => _acquiredSystemWallClockDateTime;
        public long AcqSysTicksPerSecond => _acqSysTicksPerSecond;
        public string PortableToStampedFromMonotonicStringified => _portableToStampedFromMonotonicStringified;
        public string PortableCastFromMonotonicStringified => _portableCastFromMonotonicStringified;
        public PortableMonotonicStamp PortableToPortabledFromMonotonic => _portableToPortabledFromMonotonic;
        public PortableMonotonicStamp PortableCastFromMonotonic => _portableCastFromMonotonic;
        public DateTime UtcDateTimeFromStamp => _utcDateTimeFromStamp;
        public string StringifiedMonotonicStamp => _stringifiedMonotonicStamp;
        public bool CastEqualsToPortabled => _portableToPortabledFromMonotonic == _portableCastFromMonotonic;
        public bool CastMatchesString => string.Equals(_portableCastFromMonotonic.ToString(),
            _portableCastFromMonotonicStringified, StringComparison.Ordinal);
        public bool PortabledMatchesString => string.Equals(_portableToPortabledFromMonotonic.ToString(),
            _portableToStampedFromMonotonicStringified, StringComparison.Ordinal);
        public bool PassesAll => CastEqualsToPortabled && CastMatchesString && PortabledMatchesString;
        public bool PassesInitial => CastEqualsToPortabled;
        public bool PassesOnDeserialized => CastMatchesString && PortabledMatchesString;
        public bool SameTpsAsAcquisitionSystem => ThisSystemTicksPerSecond == _acqSysTicksPerSecond;
        public bool SameRefTicksAsAcquisitionSystem => ThisSystemReferenceTicks == _acqSysRefTicks;
        
        private Issue13StampTestPacket(long acqSysRefTicks, DateTime acquiredSystemWallClockDateTime,
            long acqSysTicksPerSecond, [NotNull] string portableToStampedFromMonotonicStringified,
            [NotNull] string portableCastFromMonotonicStringified,
            PortableMonotonicStamp portableToPortabledFromMonotonic, PortableMonotonicStamp portableCastFromMonotonic,
            DateTime utcDateTimeFromStamp, [NotNull] string stringifiedMonotonicStamp)
        {
            _acqSysRefTicks = acqSysRefTicks;
            _acquiredSystemWallClockDateTime = acquiredSystemWallClockDateTime;
            _acqSysTicksPerSecond = acqSysTicksPerSecond;
            _portableToStampedFromMonotonicStringified = portableToStampedFromMonotonicStringified ??
                                                         throw new ArgumentNullException(
                                                             nameof(portableToStampedFromMonotonicStringified));
            _portableCastFromMonotonicStringified = portableCastFromMonotonicStringified ??
                                                    throw new ArgumentNullException(
                                                        nameof(portableCastFromMonotonicStringified));
            _portableToPortabledFromMonotonic = portableToPortabledFromMonotonic;
            _portableCastFromMonotonic = portableCastFromMonotonic;
            _utcDateTimeFromStamp = utcDateTimeFromStamp;
            _stringifiedMonotonicStamp = stringifiedMonotonicStamp ??
                                         throw new ArgumentNullException(nameof(stringifiedMonotonicStamp));
        }

        static Issue13StampTestPacket()
        {
            var context = MonotonicTimeStampUtil<MonotonicContext>.StampNow.Context;
            ThisSystemReferenceTicks = context.ReferenceTicks;
            ThisSystemTicksPerSecond = context.TicksPerSecond;
            ThisSystemReferenceTimeUtc = context.UtcDateTimeBeginReference;
            
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hash = _acqSysRefTicks.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _acqSysTicksPerSecond.GetHashCode();
                hash = (hash * 397) ^ _acquiredSystemWallClockDateTime.GetHashCode();
                hash = (hash * 397) ^ string.GetHashCode(_stringifiedMonotonicStamp, StringComparison.Ordinal);
            }
            return hash;
        }

        public static int Compare(in Issue13StampTestPacket lhs, in Issue13StampTestPacket rhs)
        {
            int ret;
            int stringifiedMonoComp = string.Compare(lhs._stringifiedMonotonicStamp, rhs._stringifiedMonotonicStamp,
                StringComparison.Ordinal);
            if (stringifiedMonoComp == 0)
            {
                int refTicksComp = lhs._acqSysRefTicks.CompareTo(rhs._acqSysRefTicks);
                if (refTicksComp == 0)
                {
                    int wallClockComp =
                        lhs._acquiredSystemWallClockDateTime.CompareTo(rhs._acquiredSystemWallClockDateTime);
                    if (wallClockComp == 0)
                    {
                        int toPortabledComp = PortableMonotonicStamp.Compare(in lhs._portableToPortabledFromMonotonic,
                            in rhs._portableToPortabledFromMonotonic);
                        if (toPortabledComp == 0)
                        {
                            int castComp = PortableMonotonicStamp.Compare(in lhs._portableCastFromMonotonic,
                                in rhs._portableCastFromMonotonic);
                            if (castComp == 0)
                            {
                                int utcComp = lhs._utcDateTimeFromStamp.CompareTo(rhs._utcDateTimeFromStamp);
                                if (utcComp == 0)
                                {
                                    int castStrComp = string.Compare(lhs._portableCastFromMonotonicStringified,
                                        rhs._portableCastFromMonotonicStringified, StringComparison.Ordinal);
                                    if (castStrComp == 0)
                                    {
                                        int portabledStrComp = string.Compare(
                                            lhs._portableToStampedFromMonotonicStringified,
                                            rhs._portableToStampedFromMonotonicStringified, StringComparison.Ordinal);
                                        ret = portabledStrComp == 0
                                            ? lhs._acqSysTicksPerSecond.CompareTo(rhs._acqSysTicksPerSecond)
                                            : portabledStrComp;
                                    }
                                    else
                                    {
                                        ret = castStrComp;
                                    }
                                }
                                else
                                {
                                    ret = utcComp;
                                }
                            }
                            else
                            {
                                ret = castComp;
                            }
                        }
                        else
                        {
                            ret = toPortabledComp;
                        }
                    }
                    else
                    {
                        ret = wallClockComp;
                    }
                }
                else
                {
                    ret = refTicksComp;
                }
            }
            else
            {
                ret = stringifiedMonoComp;
            }
            return ret;
        }

        public static bool operator ==(in Issue13StampTestPacket lhs, in Issue13StampTestPacket rhs) =>
            lhs._acqSysRefTicks == rhs._acqSysRefTicks &&
            lhs._acquiredSystemWallClockDateTime == rhs._acquiredSystemWallClockDateTime &&
            lhs._acqSysTicksPerSecond == rhs._acqSysTicksPerSecond &&
            string.Equals(lhs._portableCastFromMonotonicStringified, rhs._portableCastFromMonotonicStringified,
                StringComparison.Ordinal) &&
            string.Equals(lhs._portableToStampedFromMonotonicStringified,
                rhs._portableToStampedFromMonotonicStringified, StringComparison.Ordinal) &&
            lhs._portableToPortabledFromMonotonic == rhs._portableToPortabledFromMonotonic &&
            lhs._portableCastFromMonotonic == rhs._portableCastFromMonotonic &&
            lhs._utcDateTimeFromStamp == rhs._utcDateTimeFromStamp &&
            string.Equals(lhs._stringifiedMonotonicStamp, rhs._stringifiedMonotonicStamp, StringComparison.Ordinal);

        public static bool operator !=(in Issue13StampTestPacket lhs, in Issue13StampTestPacket rhs) => !(lhs == rhs);
        public static bool operator >(in Issue13StampTestPacket lhs, in Issue13StampTestPacket rhs) =>
            Compare(in lhs, in rhs) > 0;
        public static bool operator <(in Issue13StampTestPacket lhs, in Issue13StampTestPacket rhs) =>
            Compare(in lhs, in rhs) < 0;
        public static bool operator >=(in Issue13StampTestPacket lhs, in Issue13StampTestPacket rhs) => !(lhs < rhs);
        public static bool operator <=(in Issue13StampTestPacket lhs, in Issue13StampTestPacket rhs) => !(lhs > rhs);
        public override bool Equals(object other) => other is Issue13StampTestPacket i13Tp && i13Tp == this;
        public bool Equals(Issue13StampTestPacket other) => other == this;
        public int CompareTo(Issue13StampTestPacket other) => Compare(in this, in other);

        public override string ToString() =>
            $"Monotonic stamp: [{_stringifiedMonotonicStamp}], acquired at [{_acquiredSystemWallClockDateTime:O}].  " +
            $"Acquisition system ref ticks: [{_acqSysRefTicks:N}], acquisition system ticks per second {_acqSysTicksPerSecond:N}." +
            $"  Portable cast stamp: [{_portableCastFromMonotonic}], stored stringification thereof: " +
            $"[{_portableCastFromMonotonicStringified}], match?: " +
            $"[{BoolToYesNo(TestPortableAndStringificationForMatch(in _portableCastFromMonotonic, _portableCastFromMonotonicStringified))}].  " +
            $"Portable to-portabled stamp: [{_portableToPortabledFromMonotonic}]. stored stringification thereof: " +
            $"[{_portableToStampedFromMonotonicStringified}], match?: [" +
            $"{BoolToYesNo(TestPortableAndStringificationForMatch(in _portableToPortabledFromMonotonic, _portableToStampedFromMonotonicStringified))}" +
            $"].  Utc version of monotonic stamp: [{_utcDateTimeFromStamp:O}].";

        static bool TestPortableAndStringificationForMatch(in PortableMonotonicStamp stamp,
            [NotNull] string storedStringification) =>
            string.Equals(storedStringification, stamp.ToString(), StringComparison.Ordinal);

        static string BoolToYesNo(bool b) => b ? "YES" : "NO"; 

        [DataMember] private readonly long _acqSysRefTicks;
        [DataMember] private readonly DateTime _acquiredSystemWallClockDateTime;
        [DataMember] private readonly long _acqSysTicksPerSecond;
        [DataMember] [NotNull] private readonly string _portableToStampedFromMonotonicStringified;
        [DataMember] [NotNull] private readonly string _portableCastFromMonotonicStringified;
        [DataMember] private readonly PortableMonotonicStamp _portableToPortabledFromMonotonic;
        [DataMember] private readonly PortableMonotonicStamp _portableCastFromMonotonic;
        [DataMember] private readonly DateTime _utcDateTimeFromStamp;
        [DataMember] [NotNull] private readonly string _stringifiedMonotonicStamp;


        private static readonly long ThisSystemReferenceTicks;
        private static readonly long ThisSystemTicksPerSecond;
        private static readonly DateTime ThisSystemReferenceTimeUtc;
    }

    [DataContract]
    public sealed class ByRefRoList<T> : IByRefRoList<T> where T : struct
    {
        public static ByRefRoList<T> EmptyList { get; } = new ByRefRoList<T>();
        internal static ByRefRoList<T> WrapExistingArray([NotNull] T[] arr) => new ByRefRoList<T>(arr);


        /// <inheritdoc />
        public int Count => _array.Length;
        /// <inheritdoc />
        T IReadOnlyList<T>.this[int index] => this[index];
        /// <inheritdoc />
        public ref readonly T this[int idx] => ref _array[idx];

        /// <inheritdoc />
        public StructArrayByRefEnumerator<T> GetEnumerator() => StructArrayByRefEnumerator<T>.CreateEnumerator(_array);

        /// <inheritdoc />
        public bool Contains(in T value, ByRefEqTest<T> eqTester)
        {
            foreach (ref readonly var item in this)
            {
                if (eqTester(in value, in item))
                    return true;
            }

            return false;
        }

        public ByRefRoList(IEnumerable<T> source) =>
            _array = source?.ToArray() ?? throw new ArgumentNullException(nameof(source));

        private ByRefRoList([NotNull] T[] arr) => _array = arr ?? throw new ArgumentNullException(nameof(arr));

        private ByRefRoList() => _array = Array.Empty<T>();

        /// <inheritdoc />
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();


        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        [NotNull] [DataMember] private readonly T[] _array;

    }

    public delegate bool ByRefEqTest<T>(in T l, in T r);

    public interface IByRefRoList<T> : IReadOnlyList<T> where T : struct
    {
        /// <summary>
        /// Returns item at specified index by readonly reference
        /// </summary>
        /// <param name="idx">the index</param>
        /// <returns>the item at the specified index</returns>
        /// <exception cref="IndexOutOfRangeException"><paramref name="idx"/> was out range.</exception>
        new ref readonly T this[int idx] { get; }

        /// <summary>
        /// Get an enumerator that can enumerate elements by readonly reference
        /// </summary>
        /// <returns>An enumerator</returns>
        new StructArrayByRefEnumerator<T> GetEnumerator();

        /// <summary>
        /// Check if the collection contains the value in question.
        /// </summary>
        /// <param name="value">the value to check for.</param>
        /// <param name="eqTester">function to use to check equality</param>
        /// <returns>true if item found, false otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="eqTester"/> was null.</exception>
        bool Contains(in T value, [NotNull] ByRefEqTest<T> eqTester);
    }

    public struct StructArrayByRefEnumerator<T> : IEnumerator<T> where T : struct
    {
        public static StructArrayByRefEnumerator<T> CreateEnumerator([NotNull] T[] arr) =>
            new StructArrayByRefEnumerator<T>(arr ?? throw new ArgumentNullException(nameof(arr)));

        public readonly ref readonly T Current => ref _arr[_idx];
        T IEnumerator<T>.Current => Current;

        object IEnumerator.Current => _idx > -1 && _idx < _arr?.Length
            ? Current
            : throw new InvalidOperationException("Enumerator invalid or does not currently refer to any object.");

        public bool MoveNext()
        {
            ++_idx;
            return _idx > -1 && _idx < _arr.Length;
        }

        public void Reset() => _idx = -1;

        public void Dispose() {}

        private StructArrayByRefEnumerator([NotNull] T[] arr)
        {
            _arr = arr ?? throw new ArgumentNullException(nameof(arr));
            _idx = -1;
        }

        private int _idx;
        [NotNull] private readonly T[] _arr;
    }
}
