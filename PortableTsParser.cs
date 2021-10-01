using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

[assembly: InternalsVisibleTo("UnitTests")]
namespace HpTimeStamps
{
    internal ref struct PortableTsParser
    {
        public static (DateTime StampWithoutFractionalSeconds, int Nanoseconds)
            ParseStringifiedPortableStampToDtAndNano([NotNull] string parseMe) =>
            ParseStringifiedPortableStampToDtAndNano((parseMe ?? throw new ArgumentNullException(nameof(parseMe)))
                .AsSpan());

        public static (DateTime StampWithoutFractionalSeconds, int Nanoseconds)
            ParseStringifiedPortableStampToDtAndNano(in ReadOnlySpan<char> parseMe)
        {
            try
            {
                var temp = new PortableTsParser(in parseMe);
                temp.Validate();
                return temp.Parse;
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            catch (InvalidPortableStampStringException)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidPortableStampStringException(parseMe.ToString(),
                    $"Invalid argument passed to {nameof(ParseStringifiedPortableStampToDtAndNano)}.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidPortableStampStringException(parseMe.ToString(),
                    "Unexpected fault parsing portable monotonic stamp.", ex);
            }
        }

        public static int ParseIntegerValueNoLeadingZeroNoPunctOnlyDigits(ReadOnlySpan<char> value)
        {
            int sum = 0;
            int factor = 1;
            for (int i = value.Length - 1; i > -1; --i)
            {
                int digitVal = value[i] - 48;
                Debug.Assert(digitVal >= 0 && digitVal <= 9);
                sum += (digitVal * factor);
                factor *= 10;
            }

            return sum;
        }

        private (DateTime StampNoFracSeconds, int Nanoseconds) Parse => (_parseRes ??= ParseVal());

        private (DateTime StampNoFracSeconds, int Nanoseconds) ParseVal()
        {
            DateTime stamp;
            int nano;
            if (Tail[0] == 'Z')
            {
                stamp = new DateTime(YearValue, MonthValue, DayValue, HoursValue, MinutesValue, SecondsValue,
                    DateTimeKind.Utc);
                nano = 0;
            }
            else
            {
                Debug.Assert(Tail[0] == '.');
                Debug.Assert(char.IsDigit(Tail[1]));
                Debug.Assert(Tail[Tail.Length -1] == 'Z');
                var tailDigitSlice = Tail.Slice(1, Tail.Length - 2);
                Debug.Assert(Tail[1] == tailDigitSlice[0]);
                Debug.Assert(char.IsDigit(tailDigitSlice[tailDigitSlice.Length -1]));
                Span<char> temp = stackalloc char[MinLength];
                temp[temp.Length - 1] = 'Z';
                for (int i = 0; i < _span.Length && i < temp.Length - 1; ++i)
                {
                    temp[i] = _span[i];
                }

                stamp = new DateTime(YearValue, MonthValue, DayValue, HoursValue, MinutesValue, SecondsValue,
                    DateTimeKind.Utc);
                nano = ExtractNanoseconds(tailDigitSlice);
            }

            return (stamp, nano);
        }
        private int ExtractNanoseconds(ReadOnlySpan<char> tailDigitSlice)
        {
            const int nanosecondsDigits = 9;
            (int numLz, int numDigWithLz) = ExtractData(tailDigitSlice, out var withoutLeadingZeroes);
            Debug.Assert(numLz > -1);
            Debug.Assert(!withoutLeadingZeroes.IsEmpty && !withoutLeadingZeroes.IsWhiteSpace());
            Debug.Assert(numLz <= numDigWithLz);
            Debug.Assert(numLz < numDigWithLz || (withoutLeadingZeroes.Length == 1 && withoutLeadingZeroes[0] == '0'));
            Debug.Assert(AllDigitsNoLeadingZeroesUnlessZero(withoutLeadingZeroes));
            int ret;

            if (numDigWithLz == 1 && withoutLeadingZeroes[0] == '0')
            {
                ret = 0;
            }
            else
            {
                int numDig = numDigWithLz - numLz;
                Debug.Assert(numDig == withoutLeadingZeroes.Length);
                //0842084 -> 84,208,400
                //  -> withoutLeadingZeros: 842084, withoutLeadingZeros.Length: 6
                //  -> numLz: 1
                //  -> target length: 8 == (nanosecondsDigits - nlz)
                //  -> pad_amount := (target_length - withoutLeadingZeros.Length) == 2
                //  -> toNanoFactor := Pow(10, (uint) pad_amount) == Pow(10, 2u) == 100
                //  -> int parsed := (int.Parse(withoutLeadingZeroes) * toNanoFactor) == (842084 * 100) == 84_208_400)
                //000_000_001 -> 1
                //  -> withoutLeadingZeros: 1, withoutLeadingZeros.Length: 1
                //  -> numLz: 8
                //  -> target length: 1 == (nanosecondsDigits - nlz)
                //  -> pad_amount := (target_length - withoutLeadingZeros.Length) == 0
                //  -> toNanoFactor := Pow(10, (uint) pad_amount) == Pow(10, 0) == 1
                //  -> int parsed := (int.Parse(withoutLeadingZeroes) * toNanoFactor) == (1 * 1) == 1)
                ////000_000_010 -> 10
                //  -> withoutLeadingZeros: 10, withoutLeadingZeros.Length: 2
                //  -> numLz: 7
                //  -> target length: 2 == (9 - 7)
                //  -> pad_amount := (target_length - withoutLeadingZeros.Length) == 0
                //  -> toNanoFactor := Pow(10, (uint) pad_amount) == Pow(10, 0) == 1
                //  -> int parsed := (int.Parse(withoutLeadingZeroes) * toNanoFactor) == (10 * 1) == 10)

                int targetLength = nanosecondsDigits - numLz;
                int padAmount = (targetLength - numDig);
                Debug.Assert(padAmount >= 0);
                int toNanoFactor = Pow(10, (uint)padAmount);
                ret = ParseIntegerValueNoLeadingZeroNoPunctOnlyDigits(withoutLeadingZeroes) * toNanoFactor;
            }

            return ret;

            static bool AllDigitsNoLeadingZeroesUnlessZero(ReadOnlySpan<char> span)
            {
                if (span.Length == 1)
                    return char.IsDigit(span[0]);
                if (span[0] == '0')
                    return span.Length == 1;
                if (span.Length < 1)
                    return false;

                foreach (var c in span)
                {
                    if (!char.IsDigit(c))
                        return false;
                }

                return true;
            }
            static (int NumLeadingZeroes, int NumDigits) ExtractData(ReadOnlySpan<char> ts, out ReadOnlySpan<char> withoutLeadingZeroes)
            {
                int nd = 0;
                int nlz = 0;
                int? idxFNlz = null;
                for (int i = 0; i < ts.Length; ++i)
                {
                    char c = ts[i];
                    Debug.Assert(char.IsDigit(ts[i]));
                    ++nd;
                    if (idxFNlz == null)
                    {
                        if (c == '0')
                            ++nlz;
                        else
                            idxFNlz = i;
                    }
                }
                Debug.Assert(nd == ts.Length);
                Debug.Assert(nlz <= ts.Length);
                withoutLeadingZeroes = idxFNlz.HasValue ? ts.Slice(idxFNlz.Value) :  "0".AsSpan();
                return (nlz, nd);
            }

            static int Pow(int radix, uint exponent)
            {
                int ret = 1;
                while (exponent != 0)
                {
                    if ((exponent & 1) == 1)
                        ret *= radix;
                    radix *= radix;
                    exponent >>= 1;
                }
                return ret;
            }
        }

        private ReadOnlySpan<char> Year => _span.Slice(YearStartIndex, YearLength);

        private int YearValue => ParseIntegerValueNoLeadingZeroNoPunctOnlyDigits(Year.TrimStart('0'));
        private ReadOnlySpan<char> Month => _span.Slice(MonthStartIndex, MonthLength);
        private int MonthValue => ParseIntegerValueNoLeadingZeroNoPunctOnlyDigits(Month.TrimStart('0'));
        private ReadOnlySpan<char> Day => _span.Slice(DayStartIndex, DayLength);
        private int DayValue => ParseIntegerValueNoLeadingZeroNoPunctOnlyDigits(Day.TrimStart('0'));
        private ReadOnlySpan<char> Hours => _span.Slice(HourStartIndex, HourLength);
        private int HoursValue => ParseIntegerValueNoLeadingZeroNoPunctOnlyDigits(Hours.TrimStart('0'));
        private ReadOnlySpan<char> Minutes => _span.Slice(MinuteStartIndex, MinuteLength);
        private int MinutesValue => ParseIntegerValueNoLeadingZeroNoPunctOnlyDigits(Minutes.TrimStart('0'));
        private ReadOnlySpan<char> Seconds => _span.Slice(SecondsStartIndex, SecondsLength);
        private int SecondsValue => ParseIntegerValueNoLeadingZeroNoPunctOnlyDigits(Seconds.TrimStart('0'));
        private ReadOnlySpan<char> Tail => _span.Slice(PeriodOrZIndex);

        private PortableTsParser([NotNull] string portableStampString) : this(
            (portableStampString ?? 
             throw new ArgumentNullException(nameof(portableStampString))).AsSpan()) {}

        private PortableTsParser(in ReadOnlySpan<char> portableStampString)
        {
            ReadOnlySpan<char> zText = stackalloc char[1] {'Z'};
            if (portableStampString.IsWhiteSpace() || portableStampString.IsEmpty)
                throw new ArgumentException("Can't be empty or whitespace.", nameof(portableStampString));
            if (portableStampString.Length < MinLength || portableStampString.Length > MaxLength)
                throw new ArgumentException(
                    $"Parameter must have at least {MinLength} characters and no more than {MaxLength} characters.  Actual char count: {portableStampString.Length}.",
                    nameof(portableStampString));
            if (!portableStampString.EndsWith(zText, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidPortableStampStringException(portableStampString.ToString(), "Does not end with Z.");
            }
            _span = portableStampString;
            _parseRes = null;
        }

        void Validate()
        {

            //years validate
            foreach (char c in Year)
            {
                if (!char.IsDigit(c))
                    throw new InvalidPortableStampStringException(_span.ToString(), "Non-digit found in years segment.");
            }
            if (_span[FirstHyphenIndex] != '-')
                throw new InvalidPortableStampStringException(_span.ToString(),
                    $"Hyphen not found at index {FirstHyphenIndex}. (value: {_span[FirstHyphenIndex]}).");
            foreach (char c in Month)
            {
                if (!char.IsDigit(c))
                    throw new InvalidPortableStampStringException(_span.ToString(), "Non-digit found in month segment.");
            }
            if (_span[FirstHyphenIndex] != '-')
                throw new InvalidPortableStampStringException(_span.ToString(),
                    $"Hyphen not found at index {SecondHyphenIndex}. (value: {_span[SecondHyphenIndex]}).");
            foreach (char c in Day)
            {
                if (!char.IsDigit(c))
                    throw new InvalidPortableStampStringException(_span.ToString(), "Non-digit found in day segment.");
            }

            if (_span[TimeSeparatorIndex] != 'T')
                throw new InvalidPortableStampStringException(_span.ToString(),
                    $"'T' not found at index {TimeSeparatorIndex}. (value: {_span[TimeSeparatorIndex]}).");
            foreach (char c in Hours)
            {
                if (!char.IsDigit(c))
                    throw new InvalidPortableStampStringException(_span.ToString(),"Non-digit found in hours segment.");
            }
            if (_span[FirstColonIndex] != ':')
                throw new InvalidPortableStampStringException(_span.ToString(),
                    $"Colon not found at index {FirstColonIndex}. (value: {_span[FirstColonIndex]}).");
            foreach (char c in Minutes)
            {
                if (!char.IsDigit(c))
                    throw new InvalidPortableStampStringException(_span.ToString(),"Non-digit found in minutes segment.");
            }
            if (_span[SecondColonIndex] != ':')
                throw new InvalidPortableStampStringException(_span.ToString(),
                    $"Colon not found at index {SecondColonIndex}. (value: {_span[SecondColonIndex]}).");
            foreach (char c in Seconds)
            {
                if (!char.IsDigit(c))
                    throw new InvalidPortableStampStringException(_span.ToString() ,"Non-digit found in seconds segment.");
            }
            if (_span[PeriodOrZIndex] != '.' && _span[PeriodOrZIndex] != 'Z')
                throw new InvalidPortableStampStringException(_span.ToString(),
                    $"Expected decimal place or Z at index {PeriodOrZIndex}. (value: {_span[PeriodOrZIndex]}).");
            if (_span[PeriodOrZIndex] == '.')
            {
                if (Tail.Length < 2)
                    throw new InvalidPortableStampStringException(_span.ToString(),
                        $"Stamp contains period but no digits followed by Z: {Tail.ToString()}");
                bool foundZ = false;
                int numChars = 0;
                foreach (var c in Tail.Slice(1))
                {
                    if (c == 'Z')
                    {
                        if (foundZ)
                            throw new InvalidPortableStampStringException(_span.ToString(),
                                "Expected stamp to end with Z but found characters thereafter.");
                        foundZ = true;
                    }
                    else
                    {
                        if (foundZ)
                        {
                            throw new InvalidPortableStampStringException(_span.ToString(),
                                "Expected stamp to end with Z but found characters thereafter.");
                        }
                        if (!char.IsDigit(c))
                        {
                            throw new InvalidPortableStampStringException(_span.ToString(),
                                "Expected all characters after decimal in tail to contain a Z or a digit.");
                        }

                        ++numChars;
                    }
                }

                if (numChars < 1)
                    throw new InvalidPortableStampStringException(_span.ToString(), "Expected at least one digit after decimal place.");
            }
        }


        private readonly ReadOnlySpan<char> _span;
        private (DateTime StampNoFracSeconds, int Nanoseconds)? _parseRes;

        private const int YearStartIndex = 0;
        private const int YearLength = 4;
        private const int FirstHyphenIndex = 4;
        private const int MonthStartIndex = 5;
        private const int MonthLength = 2;
        private const int SecondHyphenIndex = 7;
        private const int DayStartIndex = 8;
        private const int DayLength = 2;
        private const int TimeSeparatorIndex = 10;
        private const int HourStartIndex = 11;
        private const int HourLength = 2;
        private const int FirstColonIndex = 13;
        private const int MinuteStartIndex = 14;
        private const int MinuteLength = 2;
        private const int SecondColonIndex = 16;
        private const int SecondsStartIndex = 17;
        private const int SecondsLength = 2;
        private const int PeriodOrZIndex = 19;
        private const int MaxLength = 30;
        private const int MinLength = 20;
    }
}
