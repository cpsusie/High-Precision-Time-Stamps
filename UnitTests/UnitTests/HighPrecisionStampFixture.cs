using System;
using System.Runtime.CompilerServices;
using System.Threading;
using HpTimeStamps;

namespace UnitTests
{
    public class HighPrecisionStampFixture
    {
        public const int MillisecondsPerDay = 60 * 60 * 24 * 1000;
        public HighPrecisionTimeStampSource HpStampSource => TheSource;

        public BinaryOpCode AddOrSubtract => TheRGen.Value.Next(0, 2) == 0 ? BinaryOpCode.Add : BinaryOpCode.Subtract;

        public (TimeSpan RandomTs, Duration RandomDuration, long Milliseconds) Between1MillisecondAndOneDay
        {
            get
            {
                long milliseconds = RandomMillisecondsBetween(1, MillisecondsPerDay);
                return (TimeSpan.FromMilliseconds(milliseconds), Duration.FromMilliseconds(milliseconds), milliseconds);
            }
        }

        public readonly struct HighPrecisionTimeStampSource
        {
            public DateTime Now
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => HpTimeStamps.TimeStampSource.Now;
            }

            public DateTime UtcNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => HpTimeStamps.TimeStampSource.UtcNow;
            }

            public TimeSpan TimeSinceCalibrated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => HpTimeStamps.TimeStampSource.TimeSinceCalibration;
            }

            public bool NeedsCalibration
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => HpTimeStamps.TimeStampSource.NeedsCalibration;
            }

            public bool IsHighPrecision
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => HpTimeStamps.TimeStampSource.IsHighPrecision;
            }
            /// <summary>
            /// The offset from utc.  To get utc time from local, SUBTRACT this value from local.
            /// To get local from utc, add this value to utc.
            /// </summary>
            public TimeSpan LocalOffsetFromUtc
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => HpTimeStamps.TimeStampSource.LocalUtcOffset;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CalibrateNow() => HpTimeStamps.TimeStampSource.Calibrate();
        }

        private long RandomMillisecondsBetween(int min, int max) => TheRGen.Value.Next(min, max + 1);

        private static readonly ThreadLocal<Random> TheRGen = new ThreadLocal<Random>(() => new Random());
        private static readonly HighPrecisionTimeStampSource TheSource = new HighPrecisionTimeStampSource();
    }
}