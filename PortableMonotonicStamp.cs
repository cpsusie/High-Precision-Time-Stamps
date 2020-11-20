using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HpTimesStamps.BigMath;

namespace HpTimesStamps
{
    [DataContract]
    public readonly struct PortableMonotonicStamp : IEquatable<PortableMonotonicStamp>,
        IComparable<PortableMonotonicStamp>
    {
        /// <summary>
        /// Number of nanoseconds in a second
        /// </summary>
        public const long NanosecondsFrequency = 1_000_000_000;

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
                TimeSpan offsetFromDt = CreateTimespanFromNanoseconds(_offsetInNanoseconds);
                utcTime += offsetFromDt;
                return utcTime;
            }
            catch (ArgumentOutOfRangeException inner)
            {
                throw new PortableTimestampOverflowException("Overflow prevented conversion of portable monotonic stamp to a UTC DateTime.", inner);
            }
        }

        public MonotonicTimeStamp<TStampContext> ToLocalMonotonicStamp<TStampContext>()
            where TStampContext : unmanaged, IEquatable<TStampContext>, IComparable<TStampContext>,
            IMonotonicStampContext
        {
            ref readonly TStampContext context = ref MonotonicTimeStampUtil<TStampContext>.StampContext;
            Int128 contextRefDtFromNanoseconds =
                StopwatchTicksFromNanoseconds(_dateTimeNanosecondOffsetFromMinValueUtc);
            TimeSpan timeSpanTicksFromStopwatchTicks =
                TimeSpan.FromTicks((long) contextRefDtFromNanoseconds * (TimeSpan.TicksPerSecond / LocalStopwatchFrequency));
            Int128 myStopwatchTickOffset = _offsetInNanoseconds + (LocalStopwatchFrequency / NanosecondsFrequency);
            
            DateTime myUtcReferenceDateTime = DateTime.MinValue.ToUniversalTime() + timeSpanTicksFromStopwatchTicks;
            Duration myOffsetFromReferenceDateTime = Duration.FromStopwatchTicks(in myStopwatchTickOffset);
            

            
            throw new NotImplementedException();
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
        [DataMember] private readonly Int128 _dateTimeNanosecondOffsetFromMinValueUtc;
        [DataMember] private readonly Int128 _offsetInNanoseconds;
    }
}