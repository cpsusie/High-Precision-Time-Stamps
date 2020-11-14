using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace HpTimesStamps
{
    /// <summary>
    /// A utility designed to create monotonic timestamps
    /// </summary>
    /// <typeparam name="TStampContext">The type of context this utility uses to create timestamps.</typeparam>
    public static class MonotonicTimeStampUtil<TStampContext> where TStampContext : unmanaged, IEquatable<TStampContext>, IComparable<TStampContext>, IMonotonicStampContext
    {
        /// <summary>
        /// Retrieve a monotonic timestamp recorded right now.
        /// </summary>
        public static MonotonicTimeStamp<TStampContext> StampNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MonotonicTimeStamp<TStampContext>.CreateFromRefTicks(Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// Get a monotonic timestamp as a local date time.
        /// </summary>
        /// <remarks>
        /// If the system clock and the reference
        /// monotonic clock are out of sync (drift or adjustments made to system clock or time -- user edit,
        /// syncing of system clock, daylight savings adjustments, leap seconds, etc), may not be accurate
        /// in an ABSOLUTE sense.
        /// Subtracting/Comparing with another datetime obtained using this property
        /// IN PROCESS should be accurate.
        /// </remarks>
        public static DateTime Now
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StampNow.ToLocalDateTime();
        }

        /// <summary>
        /// Get a monotonic timestamp as a utc date time.
        /// </summary>
        /// <remarks>
        /// If the system clock and the reference
        /// monotonic clock are out of sync (drift or adjustments made to system clock or time -- user edit,
        /// syncing of system clock, leap seconds, etc), may not be accurate
        /// in an ABSOLUTE sense.
        /// Subtracting/Comparing with another datetime obtained using this property IN PROCESS
        /// should be accurate.
        /// </remarks>
        public static DateTime UtcNow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StampNow.ToUtcDateTime();
        }

        /// <summary>
        /// The stamp context available by readonly-reference.  Once set for a particular instantiation of <typeparamref name="TStampContext"/>
        /// is immutable.  This value can be set by:
        ///     - Accessing this property for the first time (will be set to <see cref="TheStampContext"/>) OR
        ///     - Calling <see cref="TrySupplyNonDefaultContext"/> or <see cref="SupplyNonDefaultContextOrThrow"/> (assuming call is successful).
        ///       For these calls to BE successful, must be called before first time this property is accessed.
        /// </summary>
        public static ref readonly TStampContext StampContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref TheStampContext.Value;
        }

        /// <summary>
        /// True if <see cref="StampContext"/> has been initialized, false otherwise.
        /// Initialization is caused by access to the <see cref="StampContext"/> property or
        /// successful calls to <see cref="TrySupplyNonDefaultContext"/> or <see cref="SupplyNonDefaultContextOrThrow"/>.
        /// </summary>
        public static bool IsContextSet => TheStampContext.IsSet;
        
        static MonotonicTimeStampUtil() => TheStampContext = new LocklessLazyWriteOnceValue<TStampContext>(InitStampContext);
        
        /// <summary>
        /// Attempt to set the stamp context to the provided value
        /// </summary>
        /// <param name="context">The context</param>
        /// <returns>True for success, false for failure</returns>
        public static bool TrySupplyNonDefaultContext(in TStampContext context)
        {
            if (!TheStampContext.IsSet)
            {
                return !context.IsInvalid && TheStampContext.TrySupplyNonDefaultValue(in context);
            }
            return false;
        }

        /// <summary>
        /// Supply a non-default context object or throw an exception
        /// </summary>
        /// <param name="context">the non-default context</param>
        /// <exception cref="ArgumentException">The supplied context is invalid.</exception>
        /// <exception cref="InvalidOperationException">The context has already been set.</exception>
        public static void SupplyNonDefaultContextOrThrow(in TStampContext context)
        {
            if (context.IsInvalid) throw new ArgumentException("The supplied stamp context is invalid.", nameof(context));
            TheStampContext.SupplyNonDefaultValueOrThrow(in context);
        }
        
        static TStampContext InitStampContext()
        {
            if (typeof(TStampContext) == typeof(MonotonicStampContext))
            {
                var ret = (TStampContext) (object) MonotonicStampContext.CreateStampContext();
                return !ret.IsInvalid ? ret : throw new ArgumentException("The stamp context created is invalid."); 
            }
            return new TStampContext();
        }

        private static readonly LocklessLazyWriteOnceValue<TStampContext> TheStampContext;
    }


   
}
