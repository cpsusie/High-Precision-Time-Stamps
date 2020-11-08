using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

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

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TStampContext"></typeparam>
    public sealed class MonotonicTimeStampUtil<TStampContext> where TStampContext : struct, IEquatable<TStampContext>, IComparable<TStampContext>, IMonotonicStampContext
    {

        private ref readonly TStampContext StampContext => ref TheStampContext.Value;

        static MonotonicTimeStampUtil()
        {
            TheStampContext = new LocklessLazyWriteOnceValue<TStampContext>(() => new TStampContext());
        }

        
        private static readonly LocklessLazyWriteOnceValue<TStampContext> TheStampContext;
    }

    public readonly struct MonotonicStampContext : IEquatable<MonotonicStampContext>, IComparable<MonotonicStampContext>
    {
        public Guid ContextId { get; }

        public DateTime UtcDateTimeBeginReference { get; }

        public DateTime LocalTimeBeginReference { get; }

        public long ReferenceTicks { get; }

        public long TicksPerSecond { get; }

        public override int GetHashCode() => ContextId.GetHashCode();
        public override bool Equals(object obj) => obj is MonotonicStampContext msc && msc == this;
        public bool Equals(MonotonicStampContext other) => other == this;
        public static bool operator ==(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            lhs.ContextId == rhs.ContextId && lhs.LocalTimeBeginReference == rhs.LocalTimeBeginReference && lhs.ReferenceTicks == rhs.ReferenceTicks;
        public static bool operator !=(in MonotonicStampContext lhs, in MonotonicStampContext rhs) => !(lhs == rhs);

        public static bool operator >(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            Compare(in lhs, in rhs) > 0;
        public static bool operator <(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            Compare(in lhs, in rhs) < 0;

        public static bool operator >=(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            !(lhs < rhs);
        public static bool operator <=(in MonotonicStampContext lhs, in MonotonicStampContext rhs) =>
            !(lhs > rhs);

        public static int Compare(in MonotonicStampContext lhs, in MonotonicStampContext rhs)
        {
            int ret;
            int idComp = lhs.ContextId.CompareTo(rhs.ContextId);
            if (idComp == 0)
            {
                int tickComp = lhs.ReferenceTicks.CompareTo(rhs.ReferenceTicks);
                ret= tickComp == 0 ? lhs.LocalTimeBeginReference.CompareTo(rhs.LocalTimeBeginReference) : tickComp;
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
