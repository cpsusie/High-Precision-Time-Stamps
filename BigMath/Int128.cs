// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Int128.cs">
//   Copyright (c) 2013 Alexander Logger. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using HpTimeStamps.BigMath.Utils;
using JetBrains.Annotations;

namespace HpTimeStamps.BigMath
{
    /// <summary>
    ///     Represents a 128-bit signed integer.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
    [DataContract]
    internal struct Int128 : IComparable<Int128>, IComparable, IEquatable<Int128>, IFormattable
    {
        [FieldOffset(0)] [DataMember]
        internal ulong _lo;
        [FieldOffset(8)] [DataMember]
        internal ulong _hi;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => "0x" + ToString("X1");

        private const ulong NegativeSignMask = 0x1UL << 63;

        /// <summary>
        ///     Gets a value that represents the number 0 (zero).
        /// </summary>
        public static readonly Int128 Zero = GetZero();

        /// <summary>
        ///     Represents the largest possible value of an Int128.
        /// </summary>
        public static readonly Int128 MaxValue = GetMaxValue();

        /// <summary>
        ///     Represents the smallest possible value of an Int128.
        /// </summary>
        public static readonly Int128 MinValue = GetMinValue();

        private static Int128 GetMaxValue() => new Int128(long.MaxValue, ulong.MaxValue);

        private static Int128 GetMinValue() => new Int128(0x8000_0000_0000_0000, 0);

        private static Int128 GetZero() => new Int128();

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(byte value)
        {
            _hi = 0;
            _lo = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public Int128(bool value)
        {
            _hi = 0;
            _lo = (ulong) (value ? 1 : 0);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(char value)
        {
            _hi = 0;
            _lo = value;
        }
        
        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(decimal value)
        {
            bool isNegative = value < 0;
            uint[] bits = decimal.GetBits(value).ConvertAll(i => (uint) i);
            uint scale = (bits[3] >> 16) & 0x1F;
            if (scale > 0)
            {
                uint[] quotient;
                MathUtils.DivModUnsigned(bits, new[] { 10U * scale }, out quotient, out _);
                bits = quotient;
            }

            _hi = bits[2];
            _lo = bits[0] | (ulong) bits[1] << 32;

            if (isNegative)
            {
                TwosComplementNegate();
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(double value)
        {
            UInt128 temp = default;
            bool negate = false;
            if (value < 0)
            {
                negate = true;
                value = -value;
            }
            if (value <= ulong.MaxValue)
            {
                temp._lo = (ulong) value;
                temp._hi = 0;
            }
            else
            {
                var shift = Math.Max((int)Math.Ceiling(Math.Log(value, 2)) - 63, 0);
                temp._lo = (ulong)(value / Math.Pow(2, shift));
                temp._hi = 0;
                temp <<= shift;
            }

            if (negate)
            {
                temp = -temp;
            }

            _hi = temp._hi;
            _lo = temp._lo;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(float value) : this((decimal) value)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(short value) : this((int) value)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(int value) : this((long) value)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(long value)
        {
            _hi = unchecked((ulong) (value < 0 ? ~0 : 0));
            _lo = (ulong) value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(sbyte value) : this((long) value)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(ushort value)
        {
            _hi = 0;
            _lo = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(uint value)
        {
            _hi = 0;
            _lo = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(ulong value)
        {
            _hi = 0;
            _lo = value;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="value">The value.</param>
        public Int128(Guid value)
        {
            var int128 = value.ToByteArray().ToInt128();
            _hi = int128.High;
            _lo = int128.Low;
        }
        
        public Int128(ulong hi, ulong lo)
        {
            _hi = hi;
            _lo = lo;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Int128" /> struct.
        /// </summary>
        /// <param name="sign">The sign.</param>
        /// <param name="intsIncoming">The ints.</param>
        public Int128(int sign, ReadOnlySpan<uint> intsIncoming)
        {
            Span<ulong> values = stackalloc ulong [2];
            int valuesIndex = 0;
            for (int intsIncomingIdx = 0; intsIncomingIdx < 4; intsIncomingIdx += 2)
            {
                int lowIncomeIdx = intsIncomingIdx;
                int highIncomeIdx = intsIncomingIdx + 1;
                uint low = lowIncomeIdx > -1 && lowIncomeIdx < intsIncoming.Length
                    ? intsIncoming[lowIncomeIdx]
                    : 0;
                uint high = highIncomeIdx > -1 && highIncomeIdx < intsIncoming.Length
                    ? intsIncoming[highIncomeIdx]
                    : 0;
                values[valuesIndex] = high;
                values[valuesIndex] <<= 32;
                values[valuesIndex] |= low;
                ++valuesIndex;
            }
            
            _hi = values[1];
            _lo = values[0];

            if (sign < 0 && (_hi > 0 || _lo > 0))
            {
                // We use here two's complement numbers representation,
                // hence such operations for negative numbers.
                TwosComplementNegate();
                _hi |= NegativeSignMask; // Ensure negative sign.
            }
        }
        /// <summary>
        /// Higher 64 bits expressed as signed integer
        /// </summary>
        internal readonly long HighSigned => unchecked((long) _hi);

        /// <summary>
        ///     Higher 64 bits (unsigned).
        /// </summary>
        public readonly ulong High => _hi;

        /// <summary>
        ///     Lower 64 bits.
        /// </summary>
        public readonly ulong Low => _lo;

        /// <summary>
        ///     Gets a number that indicates the sign (negative, positive, or zero) of the current Int128 object.
        /// </summary>
        /// <value>A number that indicates the sign of the Int128 object</value>
        public readonly int Sign
        {
            get
            {
                if (_hi == 0 && _lo == 0)
                {
                    return 0;
                }

                return ((_hi & NegativeSignMask) == 0) ? 1 : -1;
            }
        }

        /// <summary>
        ///     Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        ///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode() => _hi.GetHashCode() ^ _lo.GetHashCode();

        /// <summary>
        ///     Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        ///     true if obj has the same value as this instance; otherwise, false.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals(object obj) => obj is Int128 int128 && int128 == this;

        /// <summary>
        ///     Returns a value indicating whether this instance is equal to a specified Int64 value.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>
        ///     true if obj has the same value as this instance; otherwise, false.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Int128 obj) => _hi == obj._hi && _lo == obj._lo;

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override readonly string ToString() => ToString(null, null);

        /// <summary>
        /// Get the absolute value of this value as an unsigned integer.  If this value is negative,
        /// after casting to unsigned the value will be returned in its 2's complement form
        /// </summary>
        /// <returns>The absolute value as an unsigned integer</returns>
        [System.Diagnostics.Contracts.Pure]
        public readonly UInt128 UnsignedAbsoluteValue()
        {
            UInt128 me = new UInt128(High, Low);
            if (HighSigned < 0)
            {
                me = -me;
            }
            return me;
        }

        /// <summary>
        ///     Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="format">The format. Only x, X, g, G, d, D are supported.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information about this instance.</param>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public readonly string ToString(string format, IFormatProvider formatProvider = null)
        {
            if (formatProvider == null)
            {
                formatProvider = CultureInfo.CurrentCulture;
            }

            char ch = '\0';
            if (!string.IsNullOrEmpty(format))
            {
                ch = format[0];
                if ((ch == 'x') || (ch == 'X'))
                {
                    int min;
                    int.TryParse(format.Substring(1).Trim(), out min);
                    return this.ToBytes(false).ToHexString(ch == 'X', min, trimZeros: true);
                }
                
                if (((ch != 'G') && (ch != 'g')) && ((ch != 'D') && (ch != 'd') && (ch != 'n') && (ch != 'N')))
                {
                    throw new NotSupportedException("Not supported format: " + format);
                }
            }

            return ToString((NumberFormatInfo) formatProvider.GetFormat(typeof (NumberFormatInfo)), ch == 'n' || ch == 'N');
        }

        private readonly string ToString(NumberFormatInfo info, bool includeSeparators = false)
        {
            if (Sign == 0)
            {
                return "0";
            }

            bool negative = Sign < 0;
            

            var sb = new StringBuilder();
            var ten = new UInt128(0, 10);
            UInt128 current = ToUAbs();
            UInt128 r;
            int digitsInserted = 0;
            while (true)
            {
              
                UInt128.DivMod(current, in ten, out current, out r);
                if (r._lo > 0 || current != 0 || (sb.Length == 0))
                {
                    sb.Insert(0, (char) ('0' + r._lo));
                    if (includeSeparators && ++digitsInserted % 3 == 0)
                    {
                        sb.Insert(0, info.NumberGroupSeparator);
                    }
                }
                if (current== 0)
                {
                    break;
                }
            }
            
            
            if (sb.StartsWith(info.NumberGroupSeparator.AsSpan()))
            {
                sb.Remove(0, info.NumberGroupSeparator.Length);
            }
            
            string s = sb.ToString();
            if ((negative) && (s != "0"))
            {
                return info.NegativeSign + s;
            }

            return s;
        }

        
        
        /// <summary>
        ///     Converts the numeric value to an equivalent object. The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="conversionType">The target conversion type.</param>
        /// <param name="provider">An object that supplies culture-specific information about the conversion.</param>
        /// <param name="asLittleEndian">As little endian.</param>
        /// <param name="value">
        ///     When this method returns, contains the value that is equivalent to the numeric value, if the
        ///     conversion succeeded, or is null if the conversion failed. This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if this value was converted successfully; otherwise, false.</returns>
        public readonly bool TryConvert(Type conversionType, IFormatProvider provider, bool asLittleEndian, out object value)
        {
            if (conversionType == typeof (bool))
            {
                value = (bool) this;
                return true;
            }

            if (conversionType == typeof (byte))
            {
                value = (byte) this;
                return true;
            }

            if (conversionType == typeof (char))
            {
                value = (char) this;
                return true;
            }

            if (conversionType == typeof (decimal))
            {
                value = (decimal) this;
                return true;
            }

            if (conversionType == typeof (double))
            {
                value = (double) this;
                return true;
            }

            if (conversionType == typeof (short))
            {
                value = (short) this;
                return true;
            }

            if (conversionType == typeof (int))
            {
                value = (int) this;
                return true;
            }

            if (conversionType == typeof (long))
            {
                value = (long) this;
                return true;
            }

            if (conversionType == typeof (sbyte))
            {
                value = (sbyte) this;
                return true;
            }

            if (conversionType == typeof (float))
            {
                value = (float) this;
                return true;
            }

            if (conversionType == typeof (string))
            {
                value = ToString(null, provider);
                return true;
            }

            if (conversionType == typeof (ushort))
            {
                value = (ushort) this;
                return true;
            }

            if (conversionType == typeof (uint))
            {
                value = (uint) this;
                return true;
            }

            if (conversionType == typeof (ulong))
            {
                value = (ulong) this;
                return true;
            }

            if (conversionType == typeof (byte[]))
            {
                value = this.ToBytes(asLittleEndian);
                return true;
            }

            if (conversionType == typeof (Guid))
            {
                value = new Guid(this.ToBytes(asLittleEndian));
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        ///     Converts the string representation of a number to its Int128 equivalent.
        /// </summary>
        /// <param name="value">A string that contains a number to convert.</param>
        /// <returns>
        ///     A value that is equivalent to the number specified in the value parameter.
        /// </returns>
        public static Int128 Parse(ReadOnlySpan<char> value) => Parse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);

        /// <summary>
        ///     Converts the string representation of a number in a specified style format to its Int128 equivalent.
        /// </summary>
        /// <param name="value">A string that contains a number to convert.</param>
        /// <param name="style">A bitwise combination of the enumeration values that specify the permitted format of value.</param>
        /// <returns>
        ///     A value that is equivalent to the number specified in the value parameter.
        /// </returns>
        public static Int128 Parse(ReadOnlySpan<char> value, NumberStyles style) => Parse(value, style, NumberFormatInfo.CurrentInfo);

        /// <summary>
        ///     Converts the string representation of a number in a culture-specific format to its Int128 equivalent.
        /// </summary>
        /// <param name="value">A string that contains a number to convert.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about value.</param>
        /// <returns>
        ///     A value that is equivalent to the number specified in the value parameter.
        /// </returns>
        public static Int128 Parse(ReadOnlySpan<char> value, IFormatProvider provider) => Parse(value, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));

        /// <summary>
        ///     Converts the string representation of a number in a specified style and culture-specific format to its Int128
        ///     equivalent.
        /// </summary>
        /// <param name="value">A string that contains a number to convert.</param>
        /// <param name="style">A bitwise combination of the enumeration values that specify the permitted format of value.</param>
        /// <param name="provider">An object that provides culture-specific formatting information about value.</param>
        /// <returns>A value that is equivalent to the number specified in the value parameter.</returns>
        public static Int128 Parse(ReadOnlySpan<char> value, NumberStyles style, IFormatProvider provider)
        {
            Int128 result;
            if (!TryParse(value, style, provider, out result))
            {
                throw new ArgumentException(null, "value");
            }

            return result;
        }

        /// <summary>
        ///     Tries to convert the string representation of a number to its Int128 equivalent, and returns a value that indicates
        ///     whether the conversion succeeded..
        /// </summary>
        /// <param name="value">The string representation of a number.</param>
        /// <param name="result">
        ///     When this method returns, contains the Int128 equivalent to the number that is contained in value,
        ///     or Int128.Zero if the conversion failed. This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        ///     true if the value parameter was converted successfully; otherwise, false.
        /// </returns>
        public static bool TryParse(ReadOnlySpan<char> value, out Int128 result) => TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);

        /// <summary>
        ///     Tries to convert the string representation of a number in a specified style and culture-specific format to its
        ///     Int128 equivalent, and returns a value that indicates whether the conversion succeeded..
        /// </summary>
        /// <param name="value">
        ///     The string representation of a number. The string is interpreted using the style specified by
        ///     style.
        /// </param>
        /// <param name="style">
        ///     A bitwise combination of enumeration values that indicates the style elements that can be present
        ///     in value. A typical value to specify is NumberStyles.Integer.
        /// </param>
        /// <param name="provider">An object that supplies culture-specific formatting information about value.</param>
        /// <param name="result">
        ///     When this method returns, contains the Int128 equivalent to the number that is contained in value,
        ///     or Int128.Zero if the conversion failed. This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if the value parameter was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> value, NumberStyles style, IFormatProvider provider, out Int128 result)
        {
            result = Zero;
            if (value.IsEmpty || value.IsWhiteSpace())
            {
                return false;
            }

            ReadOnlySpan<char> x = stackalloc char[1] { 'x' };
            ReadOnlySpan<char> ohX = stackalloc char[2] { '0', 'x' };

            if (value.StartsWith(x, StringComparison.OrdinalIgnoreCase))
            {
                style |= NumberStyles.AllowHexSpecifier;
                value = value.Slice(1);
            }
            else if (value.StartsWith(ohX, StringComparison.OrdinalIgnoreCase))
            {
                style |= NumberStyles.AllowHexSpecifier;
                value = value.Slice(2);
            }

            if ((style & NumberStyles.AllowHexSpecifier) == NumberStyles.AllowHexSpecifier)
            {
                return TryParseHex(value, out result);
            }

            return TryParseNum(value, out result);
        }

        private static bool TryParseHex(ReadOnlySpan<char> value, out Int128 result)
        {
            if (value.Length > 32)
            {
                throw new OverflowException();
            }

            result = Zero;
            bool hi = false;
            int pos = 0;
            for (int i = value.Length - 1; i >= 0; i--)
            {
                char ch = value[i];
                ulong b;
                if ((ch >= '0') && (ch <= '9'))
                {
                    b = (ulong) (ch - '0');
                }
                else if ((ch >= 'A') && (ch <= 'F'))
                {
                    b = (ulong) (ch - 'A' + 10);
                }
                else if ((ch >= 'a') && (ch <= 'f'))
                {
                    b = (ulong) (ch - 'a' + 10);
                }
                else
                {
                    return false;
                }

                if (hi)
                {
                    result._hi |= b << pos;
                    pos += 4;
                }
                else
                {
                    result._lo |= b << pos;
                    pos += 4;
                    if (pos == 64)
                    {
                        pos = 0;
                        hi = true;
                    }
                }
            }
            return true;
        }

        private static bool TryParseNum(ReadOnlySpan<char> value, out Int128 result)
        {
            ReadOnlySpan<char> dash = stackalloc char[1] { '-' };
            result = Zero;
            bool isNegative = value.StartsWith(dash);
            if (isNegative)
            {
                value = value.Slice(1);
            }
            foreach (char ch in value)
            {
                byte b;
                if ((ch >= '0') && (ch <= '9'))
                {
                    b = (byte) (ch - '0');
                }
                else if (ch == ',')
                {
                    continue;
                }
                else
                {
                    return false;
                }

                result = 10*result;
                result += b;
            }

            if (isNegative)
            {
                result = 0 - result;
            }
            return true;
        }

        /// <summary>
        ///     Converts the value of this instance to an <see cref="T:System.Object" /> of the specified
        ///     <see cref="T:System.Type" /> that has an equivalent value, using the specified culture-specific formatting
        ///     information.
        /// </summary>
        /// <param name="conversionType">The <see cref="T:System.Type" /> to which the value of this instance is converted.</param>
        /// <param name="provider">
        ///     An <see cref="T:System.IFormatProvider" /> interface implementation that supplies
        ///     culture-specific formatting information.
        /// </param>
        /// <param name="asLittleEndian">As little endian.</param>
        /// <returns>
        ///     An <see cref="T:System.Object" /> instance of type <paramref name="conversionType" /> whose value is equivalent to
        ///     the value of this instance.
        /// </returns>
        public readonly object ToType(Type conversionType, IFormatProvider provider, bool asLittleEndian)
        {
            object value;
            if (TryConvert(conversionType, provider, asLittleEndian, out value))
            {
                return value;
            }

            throw new InvalidCastException();
        }

        /// <summary>
        ///     Compares the current instance with another object of the same type and returns an integer that indicates whether
        ///     the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        ///     A value that indicates the relative order of the objects being compared. The return value has these meanings: Value
        ///     Meaning Less than zero This instance is less than <paramref name="obj" />. Zero This instance is equal to
        ///     <paramref name="obj" />. Greater than zero This instance is greater than <paramref name="obj" />.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">
        ///     <paramref name="obj" /> is not the same type as this instance.
        /// </exception>
        readonly int IComparable.CompareTo(object obj) => Compare(this, obj);

        /// <summary>
        ///     Compares two Int128 values and returns an integer that indicates whether the first value is less than, equal to, or
        ///     greater than the second value.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>A signed integer that indicates the relative values of left and right, as shown in the following table.</returns>
        public static int Compare(in Int128 left, object right)
        {
            if (right is Int128 r)
            {
                return Compare(left, in r);
            }

            // NOTE: this could be optimized type per type
            if (right is bool)
            {
                return Compare(left, new Int128((bool) right));
            }

            if (right is byte)
            {
                return Compare(left, new Int128((byte) right));
            }

            if (right is char)
            {
                return Compare(left, new Int128((char) right));
            }

            if (right is decimal)
            {
                return Compare(left, new Int128((decimal) right));
            }

            if (right is double)
            {
                return Compare(left, new Int128((double) right));
            }

            if (right is short)
            {
                return Compare(left, new Int128((short) right));
            }

            if (right is int)
            {
                return Compare(left, new Int128((int) right));
            }

            if (right is long)
            {
                return Compare(left, new Int128((long) right));
            }

            if (right is sbyte)
            {
                return Compare(left, new Int128((sbyte) right));
            }

            if (right is float)
            {
                return Compare(left, new Int128((float) right));
            }

            if (right is ushort)
            {
                return Compare(left, new Int128((ushort) right));
            }

            if (right is uint)
            {
                return Compare(left, new Int128((uint) right));
            }

            if (right is ulong)
            {
                return Compare(left, new Int128((ulong) right));
            }

            var bytes = right as byte[];
            if ((bytes != null) && (bytes.Length == 16))
            {
                // TODO: ensure endian.
                return Compare(left, bytes.ToInt128());
            }

            if (right is Guid)
            {
                return Compare(left, new Int128((Guid) right));
            }

            throw new ArgumentException();
        }

        /// <summary>
        ///     Compares two 128-bit signed integer values and returns an integer that indicates whether the first value is less
        ///     than, equal to, or greater than the second value.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>
        ///     A signed number indicating the relative values of this instance and value.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare(in Int128 left, in Int128 right)
        {
            int leftSign = left.Sign;
            int rightSign = right.Sign;

            if (leftSign == 0 && rightSign == 0)
            {
                return 0;
            }

            if (leftSign >= 0 && rightSign < 0)
            {
                return 1;
            }

            if (leftSign < 0 && rightSign >= 0)
            {
                return -1;
            }

            if (left._hi != right._hi)
            {
                return left._hi.CompareTo(right._hi);
            }

            return left._lo.CompareTo(right._lo);
        }

        /// <summary>
        ///     Compares this instance to a specified 128-bit signed integer and returns an indication of their relative values.
        /// </summary>
        /// <param name="value">An integer to compare.</param>
        /// <returns>A signed number indicating the relative values of this instance and value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(Int128 value) => Compare(this, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Not()
        {
            _hi = ~_hi;
            _lo = ~_lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TwosComplementNegate()
        {
            Not();
            this++;
        }

        /// <summary>
        ///     Negates a specified Int128 value.
        /// </summary>
        /// <param name="value">The value to negate.</param>
        /// <returns>The result of the value parameter multiplied by negative one (-1).</returns>
        public static Int128 TwosComplementNegate(Int128 value)
        {
            value.TwosComplementNegate();
            return value;
        }

        /// <summary>
        ///     Gets the absolute value this object.
        /// </summary>
        /// <returns>The absolute value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Int128 ToAbs() => Abs(this);

        /// <summary>
        ///     Gets the absolute value of an Int128 object.
        /// </summary>
        /// <param name="value">A number.</param>
        /// <returns>
        ///     The absolute value.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 Abs(in Int128 value)
        {
            if (value == Int128.MinValue)
                throw new ArithmeticException(
                    "The supplied value is the minimum value and it has no " +
                    "unsigned positive equivalent in a 2's complement signed representation.");
            if (value.Sign < 0)
            {
                return -value;
            }

            return value;
        }

        [System.Diagnostics.Contracts.Pure]
        public readonly UInt128 ToUAbs() => UnsignedAbsoluteValue();
        [System.Diagnostics.Contracts.Pure]
        public static UInt128 UAbs(in Int128 value) => value.UnsignedAbsoluteValue();


        /// <summary>
        ///     Divides one Int128 value by another, returns the result, and returns the remainder in an output parameter.
        /// </summary>
        /// <param name="dividend">The value to be divided.</param>
        /// <param name="divisor">The value to divide by.</param>
        /// <returns>
        ///     The quotient of the division.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Int128 Quotient, Int128 Remainder) DivRem(in Int128 dividend, in Int128 divisor)
        {
            if (divisor == 0)
            {
                throw new DivideByZeroException();
            }

            CjmUtils.DivModImpl(in dividend, in divisor, out Int128 quotient, out Int128 remainder);
            return (quotient, remainder);
        }

        

        /// <summary>
        ///     Converts an Int128 value to an unsigned long array.
        /// </summary>
        /// <returns>
        ///     The value of the current Int128 object converted to an array of unsigned integers.
        /// </returns>
        public readonly ulong[] ToUIn64Array()
        {
            return new[] {_lo, _hi};
        }

        /// <summary>
        ///     Converts an Int128 value to an unsigned integer array.
        /// </summary>
        /// <returns>The value of the current Int128 object converted to an array of unsigned integers.</returns>
        public readonly uint[] ToUIn32Array()
        {
            var ints = new uint[4];
            ulong[] ulongs = ToUIn64Array();
            Buffer.BlockCopy(ulongs, 0, ints, 0, 16);
            return ints;
        }

        /// <summary>
        ///     Returns the product of two Int128 values.
        /// </summary>
        /// <param name="left">The first number to multiply.</param>
        /// <param name="right">The second number to multiply.</param>
        /// <returns>The product of the left and right parameters.</returns>
        internal static Int128 SlowMultiply(in Int128 left, in Int128 right)
        {
            if (left == 0 || right == 0)
            {
                return 0;
            }

            if (left == 1)
            {
                return right;
            }
            if (left == -1)
            {
                var temp = ~right;
                CjmUtils.UnsignedAddAssign(ref temp, 1);
                return temp;
            }
            if (right == 1)
            {
                return left;
            }
            if (right == -1)
            {
                var temp = ~left;
                CjmUtils.UnsignedAddAssign(ref temp, 1);
                return temp;
            }
            

            int leftSign = left.Sign;
            Int128 leftCopy = leftSign < 0 ? -left : left;
            int rightSign = right.Sign;
            Int128 rightCopy = rightSign < 0 ? -right : right;

            uint[] xInts = leftCopy.ToUIn32Array();
            uint[] yInts = rightCopy.ToUIn32Array();
            var mulInts = new uint[8];

            for (int i = 0; i < xInts.Length; i++)
            {
                int index = i;
                ulong remainder = 0;
                foreach (uint yi in yInts)
                {
                    remainder = remainder + (ulong) xInts[i]*yi + mulInts[index];
                    mulInts[index++] = (uint) remainder;
                    remainder = remainder >> 32;
                }

                while (remainder != 0)
                {
                    remainder += mulInts[index];
                    mulInts[index++] = (uint) remainder;
                    remainder = remainder >> 32;
                }
            }
            return new Int128(leftSign*rightSign, mulInts);
        }

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Boolean" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int128(bool value) => new Int128(value);

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Byte" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int128(byte value) => new Int128(value);

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Char" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int128(char value) => new Int128(value);

        /// <summary>
        ///     Performs an explicit conversion from <see cref="System.Decimal" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator Int128(decimal value) => new Int128(value);

        /// <summary>
        ///     Performs an explicit conversion from <see cref="System.Double" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator Int128(double value) => new Int128(value);

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Int16" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int128(short value) => new Int128(value);

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Int32" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int128(int value) => new Int128(value);

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.Int64" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int128(long value) => new Int128(value);

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.SByte" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int128(sbyte value) => new Int128(value);

        /// <summary>
        ///     Performs an explicit conversion from <see cref="System.Single" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator Int128(float value) => new Int128(value);

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.UInt16" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int128(ushort value) => new Int128(value);

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.UInt32" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int128(uint value) => new Int128(value);

        /// <summary>
        ///     Performs an implicit conversion from <see cref="System.UInt64" /> to <see cref="Int128" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static implicit operator Int128(ulong value) => new Int128(value);

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.Boolean" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator bool(Int128 value) => value.Sign != 0;

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.Byte" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator byte(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < byte.MinValue) || (value > byte.MaxValue))
            {
                throw new OverflowException();
            }

            return (byte) value._lo;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.Char" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator char(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return (char) 0;
            }

            if ((value < char.MinValue) || (value > char.MaxValue))
            {
                throw new OverflowException();
            }

            return (char) (ushort) value._lo;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.Decimal" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator decimal(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            return new decimal((int) (value._lo & 0xFFFFFFFF), (int) (value._lo >> 32), (int) (value._hi & 0xFFFFFFFF), value.Sign < 0, 0);
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.Double" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator double(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            double d;
            NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;
            bool isMinValue = value == MinValue;
            if (isMinValue)
            {
                var incremented = value + 1;
                if (!double.TryParse(incremented.ToString(nfi), NumberStyles.Number, nfi, out d))
                {
                    throw new OverflowException();
                }
                return d - 1.0;
            }
            if (!double.TryParse(value.ToString(nfi), NumberStyles.Number, nfi, out d))
            {
                throw new OverflowException();
            }

            return d;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.Single" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator float(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            float f;
            NumberFormatInfo nfi = CultureInfo.InvariantCulture.NumberFormat;
            if (!float.TryParse(value.ToString(nfi), NumberStyles.Number, nfi, out f))
            {
                throw new OverflowException();
            }

            return f;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.Int16" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator short(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < short.MinValue) || (value > short.MaxValue))
            {
                throw new OverflowException();
            }

            return (short) value._lo;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.Int32" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator int(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < int.MinValue) || (value > int.MaxValue))
            {
                throw new OverflowException();
            }

            return (int) value._lo;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.Int64" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator long(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < long.MinValue) || (value > long.MaxValue))
            {
                throw new OverflowException();
            }

            return (long) value._lo;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.UInt32" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator uint(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < uint.MinValue) || (value > uint.MaxValue))
            {
                throw new OverflowException();
            }

            return (uint) value._lo;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.UInt16" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator ushort(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < ushort.MinValue) || (value > ushort.MaxValue))
            {
                throw new OverflowException();
            }

            return (ushort) value._lo;
        }

        /// <summary>
        ///     Performs an explicit conversion from <see cref="Int128" /> to <see cref="System.UInt64" />.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the conversion.
        /// </returns>
        public static explicit operator ulong(in Int128 value)
        {
            if (value.Sign == 0)
            {
                return 0;
            }

            if ((value < ulong.MinValue) || (value > ulong.MaxValue))
            {
                throw new OverflowException();
            }

            return value._lo;
        }

        /// <summary>
        ///     Implements the operator &gt;.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(in Int128 left, in Int128 right) => Compare(in left, in right) > 0;

        /// <summary>
        ///     Implements the operator &lt;.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(in Int128 left, in Int128 right) => Compare(in left, in right) < 0;

        /// <summary>
        ///     Implements the operator &gt;=.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(in Int128 left, in Int128 right) => Compare(in left, in right) >= 0;

        /// <summary>
        ///     Implements the operator &lt;=.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(in Int128 left, in Int128 right) => Compare(in left, in right) <= 0;

        /// <summary>
        ///     Implements the operator !=.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Int128 left, in Int128 right) => Compare(left, right) != 0;

        /// <summary>
        ///     Implements the operator ==.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Int128 left, in Int128 right) => Compare(in left, in right) == 0;

        /// <summary>
        ///     Implements the operator +.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 operator +(in Int128 value) => value;

        /// <summary>
        ///     Implements the operator -.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 operator -(in Int128 value) =>
            //if (value == Int128.MinValue)
            //    throw new ArithmeticException(
            //        "Value received is the MinValue for type: cannot negate a two's complement minimum value because it has no corresponding positive value in range.");
            TwosComplementNegate(value);

        /// <summary>
        ///     Implements the operator +.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 operator +(in Int128 left, in Int128 right)
        {
            Int128 sum = left;
            sum._hi += right._hi;
            sum._lo += right._lo;

            if (sum._lo < right._lo)
            {
                sum._hi++;
            }

            return sum;
        }

        /// <summary>
        ///     Implements the operator -.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 operator -(in Int128 left, in Int128 right) => left + -right;

        ///// <summary>
        /////     Implements the operator /.
        ///// </summary>
        ///// <param name="dividend">The dividend.</param>
        ///// <param name="divisor">The divisor.</param>
        ///// <returns>
        /////     The result of the operator.
        ///// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 operator /(in Int128 dividend, in Int128 divisor)
        {
            if (divisor == 0) throw new DivideByZeroException("The divisor cannot be zero.");
            if (dividend == 0) return 0;
            CjmUtils.DivModImpl(in dividend, in divisor, out Int128 ret, out _);
            return ret;
        }

        ///// <summary>
        /////     Implements the operator %.
        ///// </summary>
        ///// <param name="dividend">The dividend.</param>
        ///// <param name="divisor">The divisor.</param>
        ///// <returns>
        /////     The result of the operator.
        ///// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 operator %(in Int128 dividend, in Int128 divisor)
        {
            if (divisor == 0) throw new DivideByZeroException("The divisor cannot be zero.");
            if (dividend == 0) return 0;
            CjmUtils.DivModImpl(in dividend, in divisor, out _, out Int128 remainder);
            return remainder;
        }

        ///// <summary>
        /////     Implements the operator %.
        ///// </summary>
        ///// <param name="dividend">The dividend.</param>
        ///// <param name="divisor">The divisor.</param>
        ///// <returns>
        /////     The result of the operator.
        ///// </returns>
        //public static Int128 operator %(in Int128 dividend, in Int128 divisor) => SlowRemainder(in dividend, in divisor);

        ///// <summary>
        /////     Implements the operator /.
        ///// </summary>
        ///// <param name="dividend">The dividend.</param>
        ///// <param name="divisor">The divisor.</param>
        ///// <returns>
        /////     The result of the operator.
        ///// </returns>
        //public static Int128 operator /(in Int128 dividend, in Int128 divisor) => SlowDivide(in dividend, in divisor);

        /// <summary>
        ///     Implements the operator *.
        /// </summary>
        /// <param name="left">The x.</param>
        /// <param name="right">The y.</param>
        /// <returns>
        ///     The result of the operator.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 operator *(in Int128 left, in Int128 right) => CjmUtils.SignedMultiply(in left, in right);

        /// <summary>
        ///     Implements the operator &gt;&gt;.
        /// </summary>
        /// <param name="lhs">The value.</param>
        /// <param name="amount">The shift.</param>
        /// <returns>The result of the operator.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 operator >>(in Int128 lhs, int amount)
        {
            // uint64_t shifts of >= 64 are undefined, so we need some special-casing.
            if (amount < 64)
            {
                if (amount != 0)
                {
                    unchecked
                    {
                        long high = lhs.HighSigned >> amount;
                        ulong low = (lhs._lo >> amount) | (lhs._hi << (64 - amount));
                        return new Int128((ulong)high, low);
                    }
                }
                return lhs;
            }

            ulong hi = 0;
            ulong lo = (ulong) (lhs.HighSigned >> (amount - 64));
            return new Int128(hi, lo);

            //if (shift == 0)
            //{
            //    return value;
            //}

            //Span<ulong> src = stackalloc ulong[2] {value._lo, value._hi};
            //Span<ulong> dst = stackalloc ulong[2] {0, 0};
            //MathUtils.ShiftRightSigned(src, dst, shift);
            //Int128 ret = default;
            //ret._hi = dst[1];
            //ret._lo = dst[0];    //lo is stored in array entry 0

            //return ret;
        }

        /// <summary>
        ///     Implements the operator &lt;&lt;.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="amount">The shift.</param>
        /// <returns>The result of the operator.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Int128 operator <<(in Int128 value, int amount)
        {
            if (amount < 64)
            {
                if (amount != 0)
                {
                    long high = (value.HighSigned << amount) | ((long) (value.Low >> (64 - amount)));
                    ulong low = (value.Low << amount);
                    unchecked
                    {
                        return new Int128((ulong)high, low);
                    }
                }
                return value;
            }

            unchecked
            {
                long h = ((long) (value.Low << (amount - 64)));
                return new Int128((ulong) h, 0);
            }
            //if (shift == 0)
            //{
            //    return value;
            //}

            //Span<ulong> src = stackalloc ulong[2] { value._lo, value._hi };
            //Span<ulong> dst = stackalloc ulong[2] { 0, 0 };
            //MathUtils.ShiftLeft(src, dst, shift);
            //Int128 ret = default;
            //ret._hi = dst[1];
            //ret._lo = dst[0];    //lo is stored in array entry 0

            //return ret;
        }

        /// <summary>
        /// XOR bitwise operator
        /// </summary>
        /// <param name="left">left hand operand</param>
        /// <param name="right">right hand operand</param>
        /// <returns>value that is <paramref name="left"/> bitwise-xor'd
        /// with <paramref name="right"/>.</returns>
        public static Int128 operator ^(in Int128 left, in Int128 right)
        {
            Int128 result = default;
            result._hi = left._hi ^ right._hi;
            result._lo = left._lo ^ right._lo;
            return result;
        }

        /// <summary>
        ///     Implements the operator |.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static Int128 operator |(in Int128 left, in Int128 right)
        {
            Int128 result = left;
            result._hi |= right._hi;
            result._lo |= right._lo;
            return result;
        }

        /// <summary>
        ///     Implements the operator &amp;.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static Int128 operator &(in Int128 left, in Int128 right)
        {
            Int128 result = left;
            result._hi &= right._hi;
            result._lo &= right._lo;
            return result;
        }

        /// <summary>
        ///     Implements the operator ~.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the operator.</returns>
        public static Int128 operator ~(in Int128 value) => new Int128(~value._hi, ~value._lo);

        /// <summary>
        ///     Implements the operator ++.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the operator.</returns>
        public static Int128 operator ++(Int128 value) => value + 1;

        /// <summary>
        ///     Implements the operator --.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the operator.</returns>
        public static Int128 operator --(Int128 value) => value - 1;
    }
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
    [DataContract]
    internal struct UInt128 : IEquatable<UInt128>, IComparable<UInt128>
    {
        public static readonly UInt128 MaxValue = new UInt128(ulong.MaxValue, ulong.MaxValue);
        public static readonly UInt128 MinValue = new UInt128(0, 0);
        public static ref readonly UInt128 Zero => ref MinValue;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt128(uint val) => new UInt128(0, val);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt128(ulong val) => new UInt128(0, val);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator UInt128(in Int128 convertMe) => new UInt128(convertMe.High, convertMe.Low);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Int128(in UInt128 convertMe) => new Int128(convertMe._hi, convertMe._lo);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(UInt128 other) => Compare(in this, in other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(UInt128 other) => other == this;
        public static bool operator ==(in UInt128 lhs, in UInt128 rhs) => lhs._hi == rhs._hi && lhs._lo == rhs._lo;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in UInt128 lhs, in UInt128 rhs) => !(lhs == rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(in UInt128 lhs, in UInt128 rhs) => lhs._hi == rhs._hi ? lhs._lo > rhs._lo : lhs._hi > rhs._hi;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(in UInt128 lhs, in UInt128 rhs) => lhs._hi == rhs._hi ? lhs._lo < rhs._lo : lhs._hi < rhs._hi;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(in UInt128 lhs, in UInt128 rhs) => !(lhs < rhs);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(in UInt128 lhs, in UInt128 rhs) => !(lhs > rhs);
        public static UInt128 operator -(in UInt128 minuend, in UInt128 subtrahend) => minuend + (-subtrahend);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator ++(UInt128 incrementMe) => incrementMe + 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator --(UInt128 decrementMe) => decrementMe - 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator +(in UInt128 value) => value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator |(in UInt128 lhs, in UInt128 rhs)
        {
            UInt128 ret = default;
            ret._hi = lhs._hi | rhs._hi;
            ret._lo = lhs._lo | rhs._lo;
            return ret;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator &(in UInt128 lhs, in UInt128 rhs)
        {
            UInt128 ret = default;
            ret._hi = lhs._hi & rhs._hi;
            ret._lo = lhs._lo & rhs._lo;
            return ret;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator ^(in UInt128 lhs, in UInt128 rhs)
        {
            UInt128 ret = default;
            ret._hi = lhs._hi ^ rhs._hi;
            ret._lo = lhs._lo ^ rhs._lo;
            return ret;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator -(in UInt128 operand)
        {
            UInt128 ret = ~operand;
            return ret + 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator ~(in UInt128 operand)
        {
            UInt128 ret = default;
            ret._hi = ~operand._hi;
            ret._lo = ~operand._lo;
            return ret;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator +(in UInt128 lhs, in UInt128 rhs)
        {
            UInt128 sum = default;
            sum._lo = lhs._lo + rhs._lo;
            sum._hi = lhs._hi + rhs._hi;
            if (sum._lo < lhs._lo || sum._lo < rhs._lo)
            {
                ++sum._hi;
            }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (UInt128 sum, bool CarryOut) AddWithCarry(in UInt128 lhs, in UInt128 rhs)
        {
            UInt128 sum = default;
            sum._lo = lhs._lo + rhs._lo;
            sum._hi = lhs._hi + rhs._hi;
            if (sum._lo < lhs._lo || sum._lo < rhs._lo)
            {
                ++sum._hi;
            }
            return (sum, sum._hi < lhs._hi || sum._hi < rhs._hi);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator *(in UInt128 lhs, in UInt128 rhs) => CjmUtils.UnsignedMultiply(in lhs, in rhs);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator <<(in UInt128 lhs, int amount)
        {
            // uint64_t shifts of >= 64 are undefined, so we will need some
            // special-casing.
            UInt128 ret = lhs;
            if (amount < 64)
            {
                if (amount != 0)
                {
                    ulong high = (lhs._hi << amount) | (lhs._lo >> (64 - amount));
                    ulong low = lhs._lo << amount;
                    ret._hi = high;
                    ret._lo = low;
                    //return MakeUint128(
                    //    (Uint128High64(lhs) << amount) | (Uint128Low64(lhs) >> (64 - amount)),
                    //    Uint128Low64(lhs) << amount);
                }
                //amount == 0 -> lhs == ret
            }
            else
            {
                //MakeUint128(Uint128Low64(lhs) << (amount - 64), 0);
                ulong high = lhs._lo << (amount - 64);
                ret._hi = high;
                ret._lo = 0;
            }
            return ret;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator >>(in UInt128 lhs, int amount)
        {
            UInt128 ret = lhs;
            // uint64_t shifts of >= 64 are undefined, so we will need some
            // special-casing.
            if (amount < 64)
            {
                if (amount != 0)
                {
                    ulong high = lhs._hi >> amount;
                    ulong lo = (lhs._lo >> amount) | (lhs._hi << (64 - amount));
                    ret._hi = high;
                    ret._lo = lo;
                    //return MakeUint128(Uint128High64(lhs) >> amount,
                    //    (Uint128Low64(lhs) >> amount) |
                    //    (Uint128High64(lhs) << (64 - amount)));
                }
                //amount == 0 -> lhs == ret
            }
            else
            {
                //return MakeUint128(0, Uint128High64(lhs) >> (amount - 64));
                ulong low = lhs._hi >> (amount - 64);
                ret._hi = 0;
                ret._lo = low;
            }
            return ret;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator /(in UInt128 dividend, in UInt128 divisor)
        {
            if (divisor == 0) throw new DivideByZeroException("The divisor cannot be zero.");
            if (dividend == 0) return 0;
            CjmUtils.DivModImpl(dividend, in divisor, out UInt128 ret, out _);
            return ret;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 operator %(in UInt128 dividend, in UInt128 divisor)
        {
            if (divisor == 0) throw new DivideByZeroException("The divisor cannot be zero.");
            if (dividend == 0) return 0;
            CjmUtils.DivModImpl(dividend, in divisor, out _, out UInt128 remainder);
            return remainder;
        }

        public static void DivMod(in UInt128 dividend, in UInt128 divisor, out UInt128 quotient, out UInt128 remainder)
        {
            if (divisor == 0) throw new DivideByZeroException("Illegal to divide by zero");
            if (dividend != 0)
            {
                CjmUtils.DivModImpl(dividend, in divisor, out quotient, out remainder);
            }
            else
            {
                quotient = Zero;
                remainder = Zero;
            }
        }
        

        //public static bool operator <<(in UInt128 lhs, int amount)
        //{

        //}
        //public static bool operator >>(in UInt128 lhs, int amount);

        public override readonly bool Equals(object other) => other is UInt128 ui128 && ui128 == this;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Compare(in UInt128 lhs, in UInt128 rhs)
        {
            if (lhs == rhs) return 0;
            return lhs > rhs ? 1 : -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override readonly int GetHashCode()
        {
            int hash = _hi.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _lo.GetHashCode();
            }
            return hash;
        }

        internal UInt128(ulong hi, ulong lo)
        {
            _lo = lo;
            _hi = hi;
        }

        
        [FieldOffset(0)] [DataMember] internal ulong _lo;
        [FieldOffset(8)] [DataMember] internal ulong _hi;
    }

    internal static class StringBuilderExtensions
    {
        public static bool StartsWith([NotNull] this StringBuilder sb, ReadOnlySpan<char> chars)
        {
            if (sb == null) throw new ArgumentNullException(nameof(sb));
            if (sb.Length == 0 || sb.Length < chars.Length) return false;
            int idx = 0;
            foreach (char c in chars)
            {
                if (c != sb[idx++])
                    return false;
            }
            return true;
        }
    }
    
}
