using System;
using JetBrains.Annotations;

namespace HpTimeStamps
{
    /// <summary>
    /// This exception is thrown to indicate that on the current system,
    /// calling <see cref="DateTime.ToUniversalTime"/> on <see cref="DateTime.MinValue"/>
    /// yields a result outside of a range this library supports.
    ///
    /// Currently, to be supported, <see cref="DateTime"/>'s minimum value, converted to
    /// universal time, must be Year: 0001, Month: January.  Thus any value after January 0001
    /// will be out of range and unsupported.
    /// </summary>
    public sealed class UnsupportedDateTimeRangeException : Exception
    {
        internal UnsupportedDateTimeRangeException([NotNull] string message)
            : base(message) { }

    }
}