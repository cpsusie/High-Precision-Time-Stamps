using System;
using System.Diagnostics;

namespace HpTimesStamps
{
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
            Debug.Assert(_threeStepFlag.Code == ThreeStepFlagCode.Complete);
        }

        private TValue _value;
        private ReadOnlyThreeStepFlag _threeStepFlag;
        private readonly Func<TValue> _ctor;
    }
}