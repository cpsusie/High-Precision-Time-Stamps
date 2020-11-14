using System;
using JetBrains.Annotations;

namespace HpTimesStamps
{
    /// <summary>
    /// Exception that is thrown to indicate a supplied delegate violated its contract
    /// with the object or function to which it was supplied by returning a null reference.
    /// </summary>
    public sealed class DelegateReturnedNullException : DelegateException
    {
        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="offendingDelegateName">name of the offending delegate</param>
        /// <param name="offendingDelegate">the offending delegate</param>
        /// <exception cref="ArgumentNullException">At least one argument was null.</exception>
        public DelegateReturnedNullException([NotNull] string offendingDelegateName, [NotNull] Delegate offendingDelegate) :
            base(
                CreateMessage(offendingDelegateName ?? throw new ArgumentNullException(nameof(offendingDelegateName)),
                    offendingDelegate ?? throw new ArgumentNullException(nameof(offendingDelegateName))),
                offendingDelegateName, offendingDelegate, null) { }

        static string CreateMessage([NotNull] string offendingDelegateName, [NotNull] Delegate offendingDelegate)
            => $"The delegate named {offendingDelegateName} of type {offendingDelegate.GetType().Name} returned a null-reference in violation of requirements.";
    }
}