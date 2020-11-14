using System;
using System.Threading;

namespace HpTimesStamps
{
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