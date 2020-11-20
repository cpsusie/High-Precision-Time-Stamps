using System;
using JetBrains.Annotations;

namespace HpTimesStamps
{
    /// <summary>
    /// Indicates that a portable timestamp cannot be converted to a local
    /// monotonic stamp or a local datetime or timespan because of overflow.
    /// </summary>
    public sealed class PortableTimestampOverflowException : ApplicationException
    {

        /// <summary>
        /// Create an exception
        /// </summary>
        public PortableTimestampOverflowException() 
            : this(null, null) {}

        /// <summary>
        /// Create an exception
        /// </summary>
        /// <param name="inner">inner exception, if applicable</param>
        public PortableTimestampOverflowException([CanBeNull] Exception inner) 
            : this(null, inner) {}

        /// <summary>
        /// Create an exception
        /// </summary>
        /// <param name="msg">extra info to go in exception message, if applicable.</param>
        public PortableTimestampOverflowException([CanBeNull] string msg) 
            : this(msg, null) {}

        /// <summary>
        /// Create an exception
        /// </summary>
        /// <param name="msg">Extra info that should go in message, if applicable</param>
        /// <param name="inner">Inner exception if applicable</param>
        public PortableTimestampOverflowException([CanBeNull] string msg, [CanBeNull] Exception inner) 
            : base(CreateMessage(msg, inner), inner) {}

        private static string CreateMessage([CanBeNull] string extraInfo, [CanBeNull] Exception inner)
        {
            string extraStr = !string.IsNullOrWhiteSpace(extraInfo)
                ? "  Extra information: \"" + extraInfo + "\"."
                : string.Empty;
            if (inner != null)
            {
                extraStr += "  Consult inner exception for details.";
            }
            return string.Format(ExMsgFrmtStr, extraStr);
        }

        private const string ExMsgFrmtStr = @"Overflow occured when attempting to perform a conversion operation.{0}";
    }
}