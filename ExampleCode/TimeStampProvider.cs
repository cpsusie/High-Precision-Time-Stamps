using System;
using System.Runtime.CompilerServices;
using HpTimeStamps;
using JetBrains.Annotations;
using MonotonicContext = HpTimeStamps.MonotonicStampContext;
using WallClock = System.DateTime;
using HpStamp = System.DateTime;
using HpStampSource = HpTimeStamps.TimeStampSource;
namespace ExampleTimestamps
{
    using MonotonicStamp = MonotonicTimeStamp<MonotonicContext>;
    using MonoStampSource = MonotonicTimeStampUtil<MonotonicContext>;

    /// <summary>
    /// Static class used for obtaining DateTimes and other timestamps
    /// </summary>
    /// <remarks>In a non-demo implementation, <see cref="s_defaultProvider"/> should be WriteOnce (with thread-safe initializer)
    /// or readonly if you don't need to dynamically change default at runtime.
    /// </remarks>
    public static partial class TimeStampProvider
    {
        /// <summary>
        /// Indicates type of stamps retrieved by <see cref="Now"/> and <see cref="UtcNow"/>
        /// properties.
        /// </summary>
        public static DefaultStampType DefaultStampType
        {
            get
            {
                DefaultStampProvider provider = s_defaultProvider;
                switch (provider)
                {
                    default:
                    case null:
                        throw new InvalidOperationException("The provider is null or not of a recognized type.");
                    case WallClockProvider _:
                        return DefaultStampType.Wall;
                    case MonotonicClockProvider _:
                        return DefaultStampType.Monotonic;
                    case HighPrecisionClockProvider _:
                        return DefaultStampType.HighPrecision;
                }
            }
        }

        /// <summary>
        /// Use the default provider to get a timestamp expressed in local time
        /// </summary>
        public static DateTime Now
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_defaultProvider.DefaultNow;
        }

        /// <summary>
        /// Use the default provider to get a timestamp in UTC time
        /// </summary>
        public static DateTime UtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => s_defaultProvider.DefaultUtcNow;
        }

        /// <summary>
        /// Get a monotonic timestamp
        /// </summary>
        public static MonotonicStamp MonoNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MonoStampSource.StampNow;
        }

        /// <summary>
        /// Get a monotonic timestamp expressed as a utc datetime
        /// </summary>
        public static DateTime MonoUtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MonoNow.ToUtcDateTime();
        }

        /// <summary>
        /// Get a monotonic timestamp expressed as a local datetime
        /// </summary>
        public static DateTime MonoLocalNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MonoNow.ToLocalDateTime();
        }

        /// <summary>
        /// Get a high precision timestamp expressed as a local time
        /// </summary>
        public static HpStamp HpNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HpStampSource.Now;
        }

        /// <summary>
        /// Get a high precision timestamp expressed in UTC
        /// </summary>
        public static DateTime HpUtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HpStampSource.UtcNow;
        }

        /// <summary>
        /// Get the wall clock time expressed as local time
        /// </summary>
        public static DateTime WallNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => WallClock.Now;
        }

        /// <summary>
        /// Get the wall clock time expressed in utc
        /// </summary>
        public static HpStamp WallUtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => WallClock.UtcNow;
        }

        /// <summary>
        /// True if, on this thread, the high precision clock needs calibration.
        /// </summary>
        public static bool HpNeedsCalibration
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HpStampSource.NeedsCalibration;
        }

        /// <summary>
        /// The amount of time elapsed since the High Precision clock's
        /// last calibration on this thread.
        /// </summary>
        public static TimeSpan TimeSinceLastCalibration
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HpStampSource.TimeSinceCalibration;
        }

        /// <summary>
        /// Monotonic stamp context used by monotonic clock.  Contains
        /// information about frequencies, conversions, a reference time, etc.
        /// </summary>
        public static ref readonly MonotonicContext MonotonicContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref MonoStampSource.StampContext;
        }

        #region Mutators
        /// <summary>
        /// Calibrate the high precision clock now
        /// </summary>
        public static void CalibrateNow() => HpStampSource.Calibrate();
        /// <summary>
        /// Make <see cref="Now"/> and <see cref="UtcNow"/> use a monotonic clock as their source
        /// </summary>
        public static void UseMonotonicDefaultStamps() => s_defaultProvider = CreateMonotonicClock();
        /// <summary>
        /// Make <see cref="Now"/> and <see cref="UtcNow"/> use a high precision clock as their source
        /// </summary>
        public static void UseHighPrecisionDefaultStamps() => s_defaultProvider = CreateHpClock();
        /// <summary>
        /// Make <see cref="Now"/> and <see cref="UtcNow"/> use the wall clock as their source.
        /// </summary>
        public static void UseWallClockDefaultStamps() => s_defaultProvider = CreateWallClock();
        #endregion
        
        /// <summary>
        /// Adjust 
        /// </summary>
        static TimeStampProvider() => s_defaultProvider = new MonotonicClockProvider();


        [NotNull] private static DefaultStampProvider s_defaultProvider;

    }

    /// <summary>
    /// Contains nested typedefs
    /// </summary>
    public static partial class TimeStampProvider
    {
        [NotNull]
        internal static DefaultStampProvider CreateWallClock() => WallClockProvider.CreateWallClockDefaultProvider();

        [NotNull]
        internal static DefaultStampProvider CreateMonotonicClock() =>
            MonotonicClockProvider.CreateMonotonicClockProvider();

        [NotNull]
        internal static DefaultStampProvider CreateHpClock() => HighPrecisionClockProvider.CreateHpClockProvider();

        private sealed class WallClockProvider : DefaultStampProvider
        {
            internal static DefaultStampProvider CreateWallClockDefaultProvider() => new WallClockProvider();

            /// <inheritdoc />
            public override DefaultStampType DefaultStamp
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => DefaultStampType.Wall;
            }
            /// <inheritdoc />
            public override HpStamp DefaultNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => TimeStampProvider.WallNow;
            }
            /// <inheritdoc />
            public override HpStamp DefaultUtcNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => TimeStampProvider.WallUtcNow;
            }
        }

        private sealed class MonotonicClockProvider : DefaultStampProvider
        {
            internal static DefaultStampProvider CreateMonotonicClockProvider() => new MonotonicClockProvider();
            /// <inheritdoc />
            public override DefaultStampType DefaultStamp
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => DefaultStampType.Monotonic;
            }
            /// <inheritdoc />
            public override HpStamp DefaultNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => TimeStampProvider.MonoNow.ToLocalDateTime();
            }
            /// <inheritdoc />
            public override HpStamp DefaultUtcNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => TimeStampProvider.MonoNow.ToUtcDateTime();
            }
        }

        private sealed class HighPrecisionClockProvider : DefaultStampProvider
        {
            internal static DefaultStampProvider CreateHpClockProvider() => new HighPrecisionClockProvider();
            /// <inheritdoc />
            public override DefaultStampType DefaultStamp
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => DefaultStampType.HighPrecision;
            }
            /// <inheritdoc />
            public override HpStamp DefaultNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => TimeStampProvider.HpNow;
            }
            /// <inheritdoc />
            public override HpStamp DefaultUtcNow
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => TimeStampProvider.HpUtcNow;
            }
        }
    }
}