using System;
using JetBrains.Annotations;

namespace HpTimesStamps
{
    /// <summary>
    /// Exception that is thrown to indicate a supplied
    /// delegate has not fulfilled its contract with the entity
    /// to which it has been supplied.
    /// </summary>
    public abstract class DelegateException : Exception
    {
        /// <summary>
        /// The offending delegate
        /// </summary>
        [NotNull] public Delegate OffendingDelegate { get; }
        /// <summary>
        /// The name of the offending delegate
        /// </summary>
        [NotNull] public string DelegateName { get; }
        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="offendingDelegateName">name of the offending delegate</param>
        /// <param name="offendingDelegate">the offending delegate</param>
        /// <param name="inner">if another exception is the cause of this exception, it should go here.</param>
        /// One or more of <exception cref="ArgumentNullException"><paramref name="message"/>, <paramref name="offendingDelegateName"/>, and
        /// <paramref name="offendingDelegate"/> was null</exception>
        protected DelegateException([NotNull] string message, [NotNull] string offendingDelegateName,
            [NotNull] Delegate offendingDelegate, [CanBeNull] Exception inner) : base(message ?? throw new ArgumentNullException(nameof(message)), inner)
        {
            OffendingDelegate = offendingDelegate ?? throw new ArgumentNullException(nameof(offendingDelegate));
            DelegateName = offendingDelegateName ?? throw new ArgumentNullException(nameof(offendingDelegateName));
        }
    }
}