using System;
using JetBrains.Annotations;

namespace HpTimeStamps
{
    /// <summary>
    /// An exception that is thrown to indicate that a delegate
    /// violated its contract with the object to which it was supplied by
    /// throwing an exception.
    /// </summary>
    public sealed class DelegateThrewException : DelegateException
    { 
        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="offendingDelegateName">The name of the offending delegate.</param>
        /// <param name="offendingDelegate">The offending delegate.</param>
        /// <param name="inner">The exception that was thrown in violation of the delegate's contract.</param>
        /// <exception cref="ArgumentNullException">One or more arguments was null.</exception>
        public DelegateThrewException([NotNull] string offendingDelegateName, [NotNull] Delegate offendingDelegate, [NotNull] Exception inner) :
            base(
                CreateMessage(offendingDelegateName ?? throw new ArgumentNullException(nameof(offendingDelegateName)),
                    offendingDelegate ?? throw new ArgumentNullException(nameof(offendingDelegateName))),
                offendingDelegateName, offendingDelegate, inner ?? throw new ArgumentNullException(nameof(inner))) { }

        static string CreateMessage([NotNull] string offendingDelegateName, [NotNull] Delegate offendingDelegate)
            =>
                $"The delegate named {offendingDelegateName} of type {offendingDelegate.GetType().Name} threw " +
                "an exception in violation of requirements.  Consult inner exception for details.";


    }
}