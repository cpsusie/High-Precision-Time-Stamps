using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using HpTimeStamps;
using HpTimeStamps.BigMath;
using JetBrains.Annotations;

namespace UnitTests
{
    public class BinaryOperationFixture
    {
        public ImmutableArray<BinaryOperation> TestCaseOneOperations => TheBinaryOperations;
        public ImmutableArray<BinaryOperation> ComparisonEdgeCaseTests => TheComparisonEdgeCases.Value;

        static BinaryOperationFixture()
        {
            TheBinaryOperations = InitBinaryOperations();
            TheComparisonEdgeCases = new LocklessLazyWriteOnceValue<ImmutableArray<BinaryOperation>>(InitComparisonEdgeCases);
        }

        private static readonly ImmutableArray<BinaryOperation> TheBinaryOperations;

        private static readonly HpTimeStamps.LocklessLazyWriteOnceValue<ImmutableArray<BinaryOperation>>
            TheComparisonEdgeCases;

        private static ImmutableArray<BinaryOperation> InitComparisonEdgeCases()
            => BinaryOpCodeParser.ParseMany(binary_operations.comp_edge_ops);
        

        private static ImmutableArray<BinaryOperation> InitBinaryOperations()
            => BinaryOpCodeParser.ParseMany(binary_operations.mul_tc1_all_bin_op);
        
    }

    public readonly struct BinaryOperation : IEquatable<BinaryOperation>, IComparable<BinaryOperation>
    {
        internal Int128 LeftOperand => _left;
        internal Int128 RightOperand => _right;
        internal BinaryOpCode OpCode => _opCode;
        internal Int128 Result => _result;
        internal BinaryOperation(BinaryOpCode opCode, in Int128 left, in Int128 right, in Int128 result)
        {
            _left = left;
            _right = right;
            _result = result;
            _opCode = opCode;
        }

        public static bool operator ==(in BinaryOperation lhs, in BinaryOperation rhs) => lhs._left == rhs._left &&
            lhs._right == rhs._right && lhs._result == rhs._result && lhs._opCode == rhs._opCode;
        public static bool operator !=(in BinaryOperation lhs, in BinaryOperation rhs) => !(lhs == rhs);
        public static bool operator >(in BinaryOperation lhs, in BinaryOperation rhs) => Compare(in lhs, in rhs) > 0;
        public static bool operator <(in BinaryOperation lhs, in BinaryOperation rhs) => Compare(in lhs, in rhs) < 0;
        public static bool operator >=(in BinaryOperation lhs, in BinaryOperation rhs) => !(lhs < rhs);
        public static bool operator <=(in BinaryOperation lhs, in BinaryOperation rhs) => !(lhs > rhs);
        public override bool Equals(object obj) => obj is BinaryOperation bo && bo == this;
        public bool Equals(BinaryOperation other) => other == this;
        public int CompareTo(BinaryOperation other) => Compare(in this, in other);
        public bool Validate() => (DoValidate()).Validated;
        internal (bool Validated, Int128 CalculatedValue) Calculated_Value() => DoValidate();
        
        public override string ToString() => $"{_left} {_opCode} {_right} == {_result}";

        private (bool Validated, Int128 Calculated) DoValidate()
        {
            Int128 calculatedResult;
            switch (_opCode)
            {
                case BinaryOpCode.Multiply:
                    calculatedResult = _left * _right;
                    break;
                case BinaryOpCode.Divide:
                    calculatedResult = _left / _right;
                    break;
                case BinaryOpCode.LeftShift:
                    Debug.Assert(_right <= 128);
                    calculatedResult = _left << (int) _right;
                    break;
                case BinaryOpCode.RightShift:
                    Debug.Assert(_right <= 128);
                    calculatedResult = _left >> (int) _right;
                    break;
                case BinaryOpCode.And:
                    calculatedResult = (_left & _right);
                    break;
                case BinaryOpCode.Or:
                    calculatedResult = (_left | _right);
                    break;
                case BinaryOpCode.Xor:
                    calculatedResult = (_left ^ _right);
                    break;
                case BinaryOpCode.Modulus:
                    calculatedResult = (_left % _right);
                    break;
                case BinaryOpCode.Add:
                    calculatedResult = _left + _right;
                    break;
                case BinaryOpCode.Subtract:
                    calculatedResult = _left - _right;
                    break;
                case BinaryOpCode.Compare:
                    calculatedResult = Int128.Compare(in _left, in _right);
                    ValidateCompareResult(in _left, in _right, (int) calculatedResult);
                    break;
                default:
                    throw new NotSupportedException($"Operation {_opCode} is not supported.");
            }

            return (_result == calculatedResult, calculatedResult);
        }

        private void ValidateCompareResult(in Int128 left, in Int128 right, int compareResult)
        {
            if (compareResult == 0)
            {
                if (left != right) throw new InvalidOperationException("Compare res 0 but not equal.");
                if (left.GetHashCode() != right.GetHashCode())
                    throw new InvalidOperationException("Compare res 0 but hashes not equal.");
                if (left > right)
                    throw new InvalidOperationException("Compare equal but left > right??!");
                if (left < right)
                    throw new InvalidOperationException("Compare equal but left < right??!");
                if (!(left <= right))
                    throw new InvalidOperationException("Compare equal but left not <= right??!");
                if (!(left >= right))
                    throw new InvalidOperationException("Compare equal but left not >= right??!");
            }
            else if (compareResult > 0)
            {
                if (left == right) throw new InvalidOperationException("Comare greater but values equal?!");
                if (!(left > right)) throw new InvalidOperationException("Compare greater but not greater?!");
                if (left < right) throw new InvalidOperationException("Compare greater but is less?!");
                if (!(left >= right)) throw new InvalidOperationException("Comare greater but values > or equal?!");
                if (left <= right) throw new InvalidOperationException("Comare greater but left <= right?!/");
            }
            else //compareResult < 0
            {
                if (left == right) throw new InvalidOperationException("Comare less but values equal?!");
                if (!(left < right)) throw new InvalidOperationException("Compare less but not less?!");
                if (left > right) throw new InvalidOperationException("Compare less but is greater?!");
                if (!(left <= right)) throw new InvalidOperationException("Comare less but values not < or equal?!");
                if (left >= right) throw new InvalidOperationException("Comare less but left >= right?!/");
            }
        }

        public override int GetHashCode()
        {
            int hash = _opCode.GetHashCode();
            unchecked
            {
                hash = (hash * 397) ^ _result.GetHashCode();
            }

            return hash;
        }
        public static int Compare(in BinaryOperation lhs, in BinaryOperation rhs)
        {
            int ret;
            int opComparison = lhs._opCode == rhs._opCode ? 0 : (lhs._opCode > rhs._opCode ? 1 : -1);
            if (opComparison == 0)
            {
                int resultComparison = Int128.Compare(in lhs._result, in rhs._result);
                if (resultComparison == 0)
                {
                    int leftComparison = Int128.Compare(in lhs._left, in rhs._left);
                    ret = leftComparison == 0 ? Int128.Compare(in lhs._right, in rhs._result) : leftComparison;
                }
                else
                {
                    ret = resultComparison;
                }
            }
            else
            {
                ret = opComparison;
            }

            return ret;
        }
        private readonly Int128 _left;
        private readonly Int128 _right;
        private readonly Int128 _result;
        private readonly BinaryOpCode _opCode;
    }

    static class BinaryOpCodeParser
    {
        public static ImmutableArray<BinaryOperation> ParseMany([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            string[] lines = text.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 1)
            {
                throw new InvalidOperationException($"Unable to parse {text} as array of binary operations.");
            }

            var bldr = ImmutableArray.CreateBuilder<BinaryOperation>(lines.Length);
            foreach (var line in lines)
            {
                bldr.Add(Parse(line));
            }

            return bldr.MoveToImmutable();
        }

        public static BinaryOperation Parse([NotNull] string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            var trimmed = text.Trim();
            var fields = trimmed.Split(";", StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 4) throw new InvalidOperationException($"Unable to parse [{text}] as a binary operation.");
            //SplitEnumerator splitUtil = new SplitEnumerator(split, ";");
            BinaryOpCode op = Enum.Parse<BinaryOpCode>(fields[0].Trim(), true);

            ulong low, high;
            var leftValues = fields[1].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            (low, high) = ParseLowHigh(leftValues);
            Int128 left = new Int128(high, low);

            var rightValues = fields[2].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            (low, high) = ParseLowHigh(rightValues);
            Int128 right = new Int128(high, low);

            var resultValues = fields[3].Split("\t", StringSplitOptions.RemoveEmptyEntries);
            (low, high) = ParseLowHigh(resultValues);
            Int128 result = new Int128(high, low);

            return new BinaryOperation(op, in left, in right, in result);

            (ulong Low, ulong High) ParseLowHigh(string[] lh)
            {
                if (lh.Length < 2)
                    throw new InvalidOperationException($"Unable to parse [{text}] as a binary operation.");
                ulong l = ulong.Parse(lh[0], NumberStyles.HexNumber);
                ulong h = ulong.Parse(lh[1], NumberStyles.HexNumber);
                return (l, h);
            }
        }

        public ref struct SplitEnumerator
        {
            public SplitEnumerator(in ReadOnlySpan<char> splitMe, [NotNull] string splitOn)
            {
                _stringToSplit = splitMe;
                _splitChar = splitOn ?? throw new ArgumentNullException(nameof(splitMe));
                _current = new ReadOnlySpan<char>();
                _good = false;
                if (string.IsNullOrEmpty(splitOn)) throw new ArgumentNullException(nameof(splitOn));
            }

            public SplitEnumerator GetEnumerator() => this;

            public ReadOnlySpan<char> Current => _current;

            public bool MoveNext()
            {
                if (!_good && _current == default)
                {
                    _current = _stringToSplit;
                    if (_current == default) return false;
                }

                int indexOf = _current.IndexOf(_splitChar);
                if (indexOf < _current.Length)
                {
                    _current = _current.Slice(0, indexOf - 1);
                    _good = true;
                }
                else
                {
                    _current = default;
                    _good = false;
                }
                return _good;
            }

            private bool _good;
            private ReadOnlySpan<char> _current;
            private readonly ReadOnlySpan<char> _stringToSplit;
            private readonly string _splitChar;
        }
    }

    public enum BinaryOpCode : byte
    {
        LeftShift, 
        RightShift,
        And, 
        Or,
        Xor, 
        Divide,
        Modulus, 
        Add,
        Subtract, 
        Multiply,
        Compare
      
    }
}
