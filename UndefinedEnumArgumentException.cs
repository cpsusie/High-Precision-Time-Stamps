using System;
using JetBrains.Annotations;

namespace HpTimeStamps
{
    /// <summary>
    /// An exception raised to indicate that the value of an argument of type <typeparamref name="TEnum"/> was
    /// not a valued defined by the <typeparamref name="TEnum"/> enumeration type.
    /// </summary>
    /// <typeparam name="TEnum">A concrete (i.e. not <see cref="System.Enum"/> or <see cref="System.ValueType"/> or <see cref="System.Object"/>, but an actual specific)
    /// enumeration type</typeparam>
    public class UndefinedEnumArgumentException<TEnum> : ArgumentException where TEnum : unmanaged, Enum
    {
        /// <summary>
        /// The undefined enum value
        /// </summary>
        public TEnum UndefinedValue { get; }

        /// <summary>
        /// The name of the parameter if known.  Empty string otherwise.
        /// </summary>
        [NotNull]
        public sealed override string ParamName => _paramName ?? string.Empty;

        /// <summary>
        /// CTOR
        /// </summary>
        /// <param name="undefinedValue">undefined enum value</param>
        public UndefinedEnumArgumentException(TEnum undefinedValue) : this(undefinedValue, null, null) {}

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="undefinedValue">The undefined enumeration value argument.</param>
        /// <param name="inner">Inner exception if any</param>
        public UndefinedEnumArgumentException(TEnum undefinedValue, [CanBeNull] Exception inner) 
            : this(undefinedValue, null, inner) { }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="undefinedValue">The undefined enumeration value argument.</param>
        /// <param name="parameterName">The name of the invalid argument parameter if known</param>
        public UndefinedEnumArgumentException(TEnum undefinedValue, [CanBeNull] string parameterName) 
            : this(undefinedValue, parameterName, null) {}
        

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="undefinedValue">The undefined enumeration value argument.</param>
        /// <param name="parameterName">The name of the invalid argument parameter if known</param>
        /// <param name="inner">inner exception if any</param>
        public UndefinedEnumArgumentException(TEnum undefinedValue, [CanBeNull] string parameterName,
            [CanBeNull] Exception inner) : base(CreateMessage(undefinedValue, parameterName, inner), inner)
        {
            UndefinedValue = undefinedValue;
            _paramName = parameterName ?? string.Empty;
        }

        /// <summary>
        /// CTOR for inheritors
        /// </summary>
        /// <param name="message">Desired message. MANDATORY</param>
        /// <param name="undefinedValue">the undefined enum value.  MANDATORY</param>
        /// <param name="parameterName">the parameter name. OPTIONAL</param>
        /// <param name="inner">inner exception.  OPTIONAL</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> was null.</exception>
        protected UndefinedEnumArgumentException([NotNull] string message, TEnum undefinedValue,
            [CanBeNull] string parameterName, [NotNull] Exception inner) : base(
            message ?? throw new ArgumentNullException(nameof(message)), inner)
        {
            UndefinedValue = undefinedValue;
            _paramName = parameterName ?? string.Empty;
        }

        [CanBeNull] private readonly string _paramName;

        [NotNull]
        static string CreateMessage(TEnum undefinedValue, [CanBeNull] string parameterName, [CanBeNull] Exception inner) =>
            "The value of parameter " +
            (!string.IsNullOrWhiteSpace(parameterName) ? parameterName + " " : string.Empty) +
            $"(value: {undefinedValue.ToString()}) is not a defined value of the {nameof(TEnum)} enumeration type." +
            (inner != null ? " Consult inner exception for details." : string.Empty);
    }
}
