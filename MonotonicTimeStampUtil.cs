﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using JetBrains.Annotations;

namespace HpTimesStamps
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMonotonicStampContext 
    {
        Guid ContextId { get; }

        DateTime UtcDateTimeBeginReference { get; }

        DateTime LocalTimeBeginReference { get; }

        long ReferenceTicks { get; }

        long TicksPerSecond { get; }
    }

    public abstract class DelegateException : Exception
    {
        [NotNull] public Delegate OffendingDelegate { get; }
        [NotNull] public string DelegateName { get; }

        protected DelegateException([NotNull] string message, [NotNull] string offendingDelegateName,
            [NotNull] Delegate offendingDelegate, [CanBeNull] Exception inner) : base(message ?? throw new ArgumentNullException(nameof(message)), inner)
        {
            OffendingDelegate = offendingDelegate ?? throw new ArgumentNullException(nameof(offendingDelegate));
            DelegateName = offendingDelegateName ?? throw new ArgumentNullException(nameof(offendingDelegateName));
        }
    }

    public sealed class DelegateThrewException : DelegateException
    { 
        public DelegateThrewException([NotNull] string offendingDelegateName, [NotNull] Delegate offendingDelegate, [CanBeNull] Exception inner) :
            base(
                CreateMessage(offendingDelegateName ?? throw new ArgumentNullException(nameof(offendingDelegateName)),
                    offendingDelegate ?? throw new ArgumentNullException(nameof(offendingDelegateName))),
                offendingDelegateName, offendingDelegate, inner) { }

        static string CreateMessage([NotNull] string offendingDelegateName, [NotNull] Delegate offendingDelegate)
            =>
                $"The delegate named {offendingDelegateName} of type {offendingDelegate.GetType().Name} threw " +
                $"an exception in violation of requirements.  Consult inner exception for details.";


    }

    public sealed class DelegateReturnedNullException : DelegateException
    {
        public DelegateReturnedNullException([NotNull] string offendingDelegateName, [NotNull] Delegate offendingDelegate) :
            base(
                CreateMessage(offendingDelegateName ?? throw new ArgumentNullException(nameof(offendingDelegateName)),
                    offendingDelegate ?? throw new ArgumentNullException(nameof(offendingDelegateName))),
                offendingDelegateName, offendingDelegate, null) { }

        static string CreateMessage([NotNull] string offendingDelegateName, [NotNull] Delegate offendingDelegate)
            => $"The delegate named {offendingDelegateName} of type {offendingDelegate.GetType().Name} returned a null-reference in violation of requirements.";
    }

    public readonly struct MonotonicTimeStamp<TStampContext> : IEquatable<TStampContext>, IComparable<TStampContext> where TStampContext : unmanaged, IEquatable<TStampContext>, IComparable<TStampContext>, IMonotonicStampContext
    {
        public ref readonly TStampContext Context => ref MonotonicTimeStampUtil<TStampContext>.StampContext; 
        static MonotonicTimeStamp()
        {
            ref readonly TStampContext context = ref MonotonicTimeStampUtil<TStampContext>.StampContext;

            ReferenceTicks = context.ReferenceTicks;
            long swTicksPerSecond = context.TicksPerSecond; //e.g. 1000
            long tsTicksPerSecond = TimeSpan.TicksPerSecond; //e.g. 100
            OffsetPeriod = context.UtcDateTimeBeginReference - context.LocalTimeBeginReference;
            TheToTsTickConversionFactor = (double) tsTicksPerSecond / swTicksPerSecond;
            
        }

        public MonotonicTimeStamp(long stopwatchTicks)
        {
            _stopWatchTicks = stopwatchTicks;
        }


        public DateTime ToLocalDateTime() => UtcReference + (TimeSpan.FromTicks(_stopWatchTicks) - ReferenceTicksAsTimeSpan) + OffsetPeriod;
        
        private TimeSpan FromTicks => TimeSpan.FromTicks((long) (TheToTsTickConversionFactor * (_stopWatchTicks - ReferenceTicks)));

        private static readonly TimeSpan ReferenceTicksAsTimeSpan; 
        private static readonly DateTime UtcReference;
        private static readonly TimeSpan OffsetPeriod;
        private static readonly double TheToTsTickConversionFactor;
        private readonly long _stopWatchTicks;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TStampContext"></typeparam>
    public static class MonotonicTimeStampUtil<TStampContext> where TStampContext : unmanaged, IEquatable<TStampContext>, IComparable<TStampContext>, IMonotonicStampContext
    {
        public static ref readonly TStampContext StampContext => ref TheStampContext.Value;

        public static bool IsContextSet => TheStampContext.IsSet;

        
        static MonotonicTimeStampUtil()
        {
            TheStampContext = new LocklessLazyWriteOnceValue<TStampContext>(InitStampContext);
        }

        public static bool TrySupplyNonDefaultContext(in TStampContext context)
        {
            if (!TheStampContext.IsSet)
            {
                return TheStampContext.TrySupplyNonDefaultValue(in context);
            }
            return false;
        }

        public static void SupplyNonDefaultContextOrThrow(in TStampContext context)
        {
            TheStampContext.SupplyNonDefaultValueOrThrow(in context);
        }
        

        static TStampContext InitStampContext() => new TStampContext();
        static MonotonicTimeStampUtil<TStampContext> InitDefault() => new MonotonicTimeStampUtil<TStampContext>();

        
        private static readonly LocklessLazyWriteOnceValue<TStampContext> TheStampContext;
    }

    internal class LocklessLazyWriteOnce<T> where T : class
    {
        public bool IsSet => _value != null;
        
        [NotNull]
        public T Value
        {
            get
            {
                T ret = _value;
                if (ret == null)
                {
                    try
                    {
                        T temp = _initializer();
                        if (temp == null)
                        {
                            throw new DelegateReturnedNullException(nameof(_initializer), _initializer);
                        }

                        Interlocked.CompareExchange(ref _value, temp, null);
                    }
                    catch (DelegateException)
                    {
                        throw;
                    }
                    catch (Exception inner)
                    {
                        throw new DelegateThrewException(nameof(_initializer), _initializer, inner);
                    }

                    ret = _value;
                }
                Debug.Assert(ret != null);
                return ret;
            }
        }

        public bool TrySetNotDefaultValue([NotNull] T nonDefaultValue)
        {
            if (nonDefaultValue == null) throw new ArgumentNullException(nameof(nonDefaultValue));
            return Interlocked.CompareExchange(ref _value, nonDefaultValue, null) == null;
        }

        public void SetNotDefaultValueOrThrow([NotNull] T nonDefaultValue)
        {
            if (!TrySetNotDefaultValue(nonDefaultValue))
            {
                throw new InvalidOperationException("The value has already been set.");
            }
        }

        public LocklessLazyWriteOnce([NotNull] Func<T> initializer) =>
            _initializer = initializer?? throw new ArgumentNullException(nameof(initializer));
        

        private readonly Func<T> _initializer;
        [CanBeNull] private volatile T _value;
    }

    public sealed class MonotonicClockNotAvailableException : ApplicationException
    {
        public MonotonicClockNotAvailableException() : base(MessageText)
        {

        }

        private const string MessageText = "No monotonic clock support is available on this platform.";
    }

    

    public readonly struct MonotonicStampContext : IEquatable<MonotonicStampContext>, IComparable<MonotonicStampContext>
    {
        public Guid ContextId { get; }

        public DateTime UtcDateTimeBeginReference { get; }

        public DateTime LocalTimeBeginReference =>
            DateTime.SpecifyKind(UtcDateTimeBeginReference + UtcOffset, DateTimeKind.Local);

        public long ReferenceTicks { get; }

        public long TicksPerSecond { get; }

        public TimeSpan UtcOffset { get; }

        public override int GetHashCode() => ContextId.GetHashCode();
        public override bool Equals(object obj) => obj is MonotonicStampContext msc && msc == this;
        public bool Equals(MonotonicStampContext other) => other == this;

        public static bool operator ==(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            lhs.ContextId == rhs.ContextId && lhs.UtcDateTimeBeginReference == rhs.UtcDateTimeBeginReference &&
            lhs.ReferenceTicks == rhs.ReferenceTicks && lhs.TicksPerSecond == rhs.TicksPerSecond &&
            lhs.UtcOffset == rhs.UtcOffset;
        public static bool operator !=(in MonotonicStampContext lhs, in MonotonicStampContext rhs) => !(lhs == rhs);

        public static bool operator >(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            Compare(in lhs, in rhs) > 0;
        public static bool operator <(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            Compare(in lhs, in rhs) < 0;

        public static bool operator >=(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            !(lhs < rhs);
        public static bool operator <=(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            !(lhs > rhs);
        public int CompareTo(MonotonicStampContext other) => Compare(in this, in other);

        static MonotonicStampContext()
        {
            if (!Stopwatch.IsHighResolution)
            {
                throw new MonotonicClockNotAvailableException();
            }
        }

        public static int Compare(in MonotonicStampContext lhs, in MonotonicStampContext rhs)
        {
            int ret;
            int idComp = lhs.ContextId.CompareTo(rhs.ContextId);
            if (idComp == 0)
            {
                int utcComp = lhs.UtcDateTimeBeginReference.CompareTo(rhs.UtcDateTimeBeginReference);
                if (utcComp == 0)
                {
                    int refTickCount = lhs.ReferenceTicks.CompareTo(rhs.ReferenceTicks);
                    if (refTickCount == 0)
                    {
                        int tpsComp = lhs.TicksPerSecond.CompareTo(rhs.TicksPerSecond);
                        ret = tpsComp == 0 ? lhs.UtcOffset.CompareTo(rhs.UtcOffset) : tpsComp;
                    }
                    else
                    {
                        ret = refTickCount;
                    }
                }
                else
                {
                    ret = utcComp;
                }
            }
            else
            {
                ret = idComp;
            }
            return ret;
        }
    }

  
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    internal sealed class LocklessLazyWriteOnceValue<TValue> where TValue : struct
    {
        /// <summary>
        /// 
        /// </summary>
        public bool IsSet => _threeStepFlag.Code == ThreeStepFlagCode.Complete;

        public ref readonly TValue Value
        {
            get
            {
                EnsureAvailability();
                return ref _value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctor"></param>
        public LocklessLazyWriteOnceValue(Func<TValue> ctor) =>
            _ctor = ctor ?? throw new ArgumentNullException(nameof(ctor));

        public void SupplyNonDefaultValueOrThrow(in TValue nonDefaultVal)
        {
            if (!TrySupplyNonDefaultValue(in nonDefaultVal))
            {
                throw new InvalidOperationException(
                    "The value has already been set either threw default initialization or by a prior explicit setting.");
            }
        }

        public bool TrySupplyNonDefaultValue(in TValue nonDefaultVal)
        {
            if (_threeStepFlag.TryBegin())
            {
                _value = nonDefaultVal;
                _threeStepFlag.CompleteOrThrow();
                return true;
            }
            return false;
        }

        private void EnsureAvailability()
        {
            while (_threeStepFlag.Code != ThreeStepFlagCode.Complete)
            {
                if (_threeStepFlag.TryBegin())
                {
                    try
                    {
                        _value = _ctor();
                    }
                    catch (Exception ex)
                    {
                        _threeStepFlag.ErrorOutOrThrow();
                        throw new InvalidOperationException(
                            "Initialization delegate threw exception.  Consult inner exception for details.", ex);
                    }
                    _threeStepFlag.CompleteOrThrow();
                }
            }
            Debug.Assert(_threeStepFlag.Code != ThreeStepFlagCode.Complete);
        }

        private TValue _value;
        private ReadOnlyThreeStepFlag _threeStepFlag;
        private readonly Func<TValue> _ctor;
    }

    
    internal struct ReadOnlyThreeStepFlag
    {
        public readonly ThreeStepFlagCode Code
        {
            get
            {
                int temp = _code;
                return (ThreeStepFlagCode)temp;
            }
        }

        public bool TryBegin()
        {
            const int wantToBe = (int)ThreeStepFlagCode.InProcess;
            const int needToBeNow = (int)ThreeStepFlagCode.Clear;
            return Interlocked.CompareExchange(ref _code, wantToBe, needToBeNow) == needToBeNow;
        }

        public bool TryErrorOut()
        {
            const int wantToBe = (int)ThreeStepFlagCode.Clear;
            const int needToBeNow = (int)ThreeStepFlagCode.InProcess;
            return Interlocked.CompareExchange(ref _code, wantToBe, needToBeNow) == needToBeNow;
        }

        public bool TryComplete()
        {
            const int wantToBe = (int)ThreeStepFlagCode.Complete;
            const int needToBeNow = (int)ThreeStepFlagCode.InProcess;
            return Interlocked.CompareExchange(ref _code, wantToBe, needToBeNow) == needToBeNow;
        }

        public void ErrorOutOrThrow()
        {
            if (!TryErrorOut()) throw new InvalidOperationException("Not in proper state to error out.");
        }

        public void CompleteOrThrow()
        {
            if (!TryComplete()) throw new InvalidOperationException("No in proper state to complete.");
        }

        public override readonly string ToString() => "ReadOnlyThreeStepFlag: [" + Code + "].";

        private volatile int _code;
    }

    internal enum ThreeStepFlagCode
    {
        Clear = 0,
        InProcess = 1,
        Complete = 2
    }
}
