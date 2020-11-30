using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;

namespace HpTimeStamps.BigMath.Utils
{
    static class CjmUtils
    {
        /// <summary>
        /// Return the bitpos of the most significant set bit (i.e. with value == 1).
        /// Adapted from Abseil.IO's ABSL_ATTRIBUTE_ALWAYS_INLINE int Fls128(uint128 n) function found at
        /// https://raw.githubusercontent.com/abseil/abseil-cpp/master/absl/numeric/int128.cc .
        /// Unlike <see cref="CountLeadingZeros"/> which I have absolutely no idea how it works, this seems pretty
        /// straight forward ... except insofar as it relies on the WTFishness of <see cref="CountLeadingZeros"/>.
        /// </summary>
        /// <param name="testMe">the value to test</param>
        /// <returns>the bitpos of the most significant bit whose value is one (0 to 127).</returns>
        /// <remarks>DO NOT SEND THIS FUNCTION ZERO AS A VALUE!</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Fls128(in Int128 testMe)
        {
            Debug.Assert(testMe != 0, "Zero is not an acceptable value!");
            ulong hi = testMe.High;
            if (hi != 0)
            {
                return 127 - CountLeadingZeros(hi);
            }

            Debug.Assert(testMe.Low != 0, "Should not be zero!");
            return 63 - CountLeadingZeros(testMe.Low);
        }

        /// <summary>
        /// I have no idea how or why this works ... I adapted it from abseil.io's
        /// function ABSL_BASE_INTERNAL_FORCEINLINE int CountLeadingZeros64Slow(uint64_t n) at
        /// https://raw.githubusercontent.com/abseil/abseil-cpp/master/absl/base/internal/bits.h.
        /// It comes from google d00dz and passes unit testing, so I'm calling it good.
        /// </summary>
        /// <param name="n">the ulong you want to get the leading zero count for.</param>
        /// <returns>the leading zero count</returns>
        /// <exception cref="ArgumentOutOfRangeException">Zero is not a permissible value.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CountLeadingZeros(ulong n)
        {
            Debug.Assert(n != 0, "May not be zero!");
            // if (n == 0) throw new ArgumentOutOfRangeException(nameof(n), n, "May not be zero.");
            unchecked
            {
                ReadOnlySpan<byte> bytes = stackalloc byte[]
                {
                    0x04, 0x03, 0x02, 0x02,
                    0x01, 0x01, 0x01, 0x01,
                    0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00
                };

                int zeroes = 60;
                if ((n >> 32) != 0)
                {
                    zeroes -= 32;
                    n >>= 32;
                }

                if ((n >> 16) != 0)
                {
                    zeroes -= 16;
                    n >>= 16;
                }

                if ((n >> 8) != 0)
                {
                    zeroes -= 8;
                    n >>= 8;
                }

                if ((n >> 4) != 0)
                {
                    zeroes -= 4;
                    n >>= 4;
                }

                return bytes[(int) n] + zeroes;
            }

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Int128 UnsignedMultiply(in Int128 lhs, in Int128 rhs)
        {
            ulong a32 = lhs.Low >> 32;
            ulong a00 = lhs.Low & 0xffffffff;
            ulong b32 = rhs.Low >> 32;
            ulong b00 = rhs.Low & 0xffffffff;

            ulong lHighTimesRLow = lhs.High * rhs.Low;
            ulong lLowTimesRHigh = lhs.Low * rhs.High;
            ulong a32TimeB32 = a32 * b32;
            ulong a0TimesB0 = a00 * b00;
            ulong a32TimesB0_64 = a32 * b00;
            ulong a00TimesB32_64 = a00 * b32;

            ulong resHigh = lHighTimesRLow + lLowTimesRHigh + a32TimeB32;
            ulong resLow = a0TimesB0;

            Int128 result = new Int128(resHigh, resLow); 
            Int128 a32TimesB00 = new Int128(a32TimesB0_64);
            Int128 a00TimesB32 = new Int128(a00TimesB32_64);
            Int128 a32TimesB00LShift32 = a32TimesB00 << 32;
            Int128 a00TimesB32LShift32 = a00TimesB32 << 32;
            
            UnsignedAddAssign(ref result, in a32TimesB00LShift32);
            UnsignedAddAssign(ref result, in a00TimesB32LShift32);

            return result;
        }

        internal static void UnsignedAddAssign(ref Int128 addToMe, in Int128 addMe)
        {
            ulong origLow = addToMe._lo;
            addToMe._hi += addMe._hi;
            addToMe._lo += addMe._lo;
            if (addToMe._lo < origLow) //check for cary
            {
                ++addToMe._hi;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Int128 SignedMultiply(in Int128 lhs, in Int128 rhs)
        {
            if (lhs == 0 || rhs == 0)
            {
                return 0;
            }

            if (lhs == 1 || rhs == 1)
            {
                return lhs == 1 ? rhs : lhs;
            }
            if (lhs == -1)
            {
                if (rhs == Int128.MinValue)
                    throw new ArithmeticException("Cannot convert min value to a positive value.");
                var temp = ~rhs;
                UnsignedAddAssign(ref temp, 1);
                return temp;
            }

            if (rhs == -1)
            {
                if (lhs == Int128.MinValue)
                    throw new ArithmeticException("Cannot convert min value to a positive value.");
                var temp = ~lhs;
                UnsignedAddAssign(ref temp, 1);
                return temp;
            }
            
            bool resultShouldBeNegative = (lhs < 0) != (rhs < 0);
            Int128 result = UnsignedMultiply(in lhs, in rhs);
            bool resultIsNegative = (result < 0);
            if (resultIsNegative != resultShouldBeNegative)
            {
                Debug.Assert(result != Int128.MinValue, "result != Int128.MinValue");
                result = -result;
            }
            return result;
        }

        internal static int Fls128Verify(in Int128 value)
        {
            if (value == 0 ) throw new ArgumentOutOfRangeException(nameof(value), value, "value may not be zero!");
            int bitpos = 127;
            Int128 mask = new Int128(0x8000_0000_0000_0000, 0x0000_0000_0000_0000);
            bool foundIt;
            do
            {
                foundIt = (value & mask) == mask;
                if (!foundIt)
                {
                    --bitpos;
                    mask = UnsignedShiftRight(in mask, 1);
                }
            } while (!foundIt);
            return bitpos;
        }

        /// <summary>
        /// Taken from abseil.io's uint128 operator>>(uint128 lhs, int amount)
        /// at <see link="https://raw.githubusercontent.com/abseil/abseil-cpp/master/absl/numeric/int128.h"/>
        /// </summary>
        /// <param name="shiftUs">value to shift</param>
        /// <param name="amount">amount to shift</param>
        /// <returns>the amount shifted right as if it were unsigned</returns>
        [Pure]
        public static Int128 UnsignedShiftRight(in Int128 shiftUs, int amount)
        {
            if (amount < 64)
            {
                if (amount != 0)
                {
                    ulong high = shiftUs.High;
                    ulong low = shiftUs.Low;
                    ulong shiftedHigh = high >> amount;
                    ulong shiftedLow = (low >> amount) | (high << (64 - amount));
                    return new Int128(shiftedHigh, shiftedLow);
                }
                return shiftUs;
            }
            return new Int128(0, shiftUs.High >> (amount - 64));
        }

        internal static int CountLeadingZeroesVerify(ulong val)
        {
            if (val == 0) return 64;

            ulong mask = 0x8000_0000_0000_0000;
            int zeroCount = 0;
            bool oneDetected = (val & mask) == mask;
            while (!oneDetected && mask != 0)
            {
                ++zeroCount;
                mask >>= 1;
                oneDetected = (val & mask) == mask;
            }
            return zeroCount;
        }
    }
}
