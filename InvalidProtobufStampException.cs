using System;
using System.Transactions;
using JetBrains.Annotations;

namespace HpTimeStamps
{
    /// <summary>
    /// Indicates that the protobuf formatted timestamp is not a valid value.
    /// </summary>
    /// <remarks>
    /// Can be thrown in response to call to <see cref="ProtobufFormatStamp"/> type's
    /// <see cref="ProtobufFormatStamp.Validate"/> method.  Will also be thrown when attempting
    /// to convert the protobuf format stamp to another type of stamp (<see cref="DateTime"/>,
    /// <see cref="MonotonicTimeStamp{TStampContext}"/> or <see cref="PortableMonotonicStamp"/>).</remarks>
    public sealed class InvalidProtobufStampException : ApplicationException
    {
        internal static void ThrowIf(long seconds, int nanos)
        {
            const string badNanosBase = "The nanoseconds component (value: {0:N0}) is invalid because";
            const string nanosTooBigFormatString =
                " it is greater than {0:N0}";
            const string nanosNegativeFormatString =
                " it is negative";
            bool negative = nanos < 0;
            bool tooBig = nanos > ProtobufFormatStamp.MaxNanos;

            if (negative || tooBig)
            {
                string baseText = string.Format(badNanosBase, nanos);
                string exMsg = (negative, tooBig, nanos) switch
                {
                    (true, false, _) => baseText + nanosNegativeFormatString + ".",
                    (true, true, _) => baseText + string.Format(nanosNegativeFormatString) + " and" +
                                           nanosNegativeFormatString + ".",
                    (_, _, var n) => baseText + string.Format(nanosTooBigFormatString, n) + "."
                };
                throw new InvalidProtobufStampException(seconds, nanos, exMsg);
            }
        }

        /// <summary>
        /// Seconds component of invalid stamp
        /// </summary>
        public long Seconds { get; }

        /// <summary>
        /// Nanoseconds component of invalid stamp
        /// </summary>
        public int Nanos { get; }
        internal InvalidProtobufStampException(long seconds, int nanos, [NotNull] string message) : this(seconds, nanos, message, null) {}
        internal InvalidProtobufStampException(long seconds, int nanos, [NotNull] string message,
            [CanBeNull] Exception inner) : base(
            CreateMessage(seconds, nanos, message ?? throw new ArgumentNullException(nameof(message)), inner),
            inner) => (Seconds, Nanos) = (seconds, nanos);

        [NotNull]
        private static string CreateMessage(long seconds, int nanos, [NotNull] string message,
            [CanBeNull] Exception inner) =>
            $"The protobuf stamp (values-- seconds: [{seconds:N0}]; nanoseconds: [{nanos:N0}]) is invalid for the following reason: \"{message}\"." +
            (inner != null ? " Consult inner exception for details." : string.Empty);
    }
}