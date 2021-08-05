using System;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace HpTimeStamps
{
    /// <summary>
    /// Exception is thrown when there is an error parsing a serialized portable monotonic timestamp
    /// </summary>
    public sealed class InvalidPortableStampStringException : SerializationException
    {
        /// <summary>
        /// The text that could not be deserialized into a portable monotonic timestamp.
        /// </summary>
        [NotNull] public string FaultySerializedText { get; }

        internal InvalidPortableStampStringException([NotNull] string toDeserialize, [NotNull] string message) : this(
            toDeserialize, message ?? throw new ArgumentNullException(nameof(message)), null) {}

        internal InvalidPortableStampStringException([NotNull] string toDeserialize, [NotNull] string message,
            [CanBeNull] Exception inner) : base(
            CreateMessage(toDeserialize ?? throw new ArgumentNullException(nameof(toDeserialize)),
                message ?? throw new ArgumentNullException(nameof(message)), inner), inner) =>
            FaultySerializedText = toDeserialize;
        

        [NotNull]
        static string CreateMessage([NotNull] string  toDeser, string msg, Exception inner) =>
            $"Error occurred when deserializing value from text \"{toDeser}\".  Additional information: \"{msg}\"." +
            (inner != null ? " Consult inner exception for details." : string.Empty);
    }
}