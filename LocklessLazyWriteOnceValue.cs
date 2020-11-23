using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace HpTimeStamps
{
    [DataContract]
    internal sealed class LocklessWriteOnceValue<TValue> where TValue : struct
    {
        public bool IsSet => _threeStapFlag.Code == ThreeStepFlagCode.Complete;
        
        public ref readonly TValue Value
        {
            get
            {
                if (_threeStapFlag.Code != ThreeStepFlagCode.Complete)
                {
                    throw new InvalidOperationException("The value has not yet been set.");
                }
                return ref _value;
            }
        }

        public bool TrySetValue(in TValue value)
        {
            if (_threeStapFlag.TryBegin())
            {
                _value = value;
                _threeStapFlag.CompleteOrThrow();
                return true;
            }
            return false;
        }

        public bool TrySetValue(TValue value) => TrySetValue(in value);

        public void SetValueOrThrow(in TValue value)
        {
            if (!TrySetValue(in value)) throw new InvalidOperationException("Value has already been set.");
        }

        public void SetValueOrThrow(TValue value) => SetValueOrThrow(in value);

        public override string ToString() =>
            IsSet ? $"LocklessWriteOnceValue.  Value: [{_value}]" : $"LocklessWriteOnceValue.  Value NOT SET.";
        

        private TValue _value;
        private ReadOnlyThreeStepFlag _threeStapFlag = default;
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
            Debug.Assert(_threeStepFlag.Code == ThreeStepFlagCode.Complete);
        }

        private TValue _value;
        private ReadOnlyThreeStepFlag _threeStepFlag;
        private readonly Func<TValue> _ctor;
    }
}