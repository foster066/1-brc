using BenchmarkDotNet.Attributes;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Benchmarks;

[MemoryDiagnoser]
public class AverageCalculationBenchmark
{
    private ReadOnlyMemory<char>[] _dataCharArray;

    [GlobalSetup]
    public void Setup()
    {
        Random rnd = new(1122);
        _dataCharArray = Enumerable
            .Range(0, 1_000_000_0)
            .Select(i => rnd.NextSingle() * 100)
            .Where(i => i > 0.01)
            .Select(i => i.ToString(CultureInfo.InvariantCulture).AsMemory())
            .Where(s => s.Length <= 5)
            .ToArray();
    }

    [Benchmark]
    public double AverageOnDoubles()
    {
        double sum = 0;
        foreach (ReadOnlyMemory<char> memory in _dataCharArray)
        {
            double.TryParse(memory.Span, CultureInfo.InvariantCulture, out var result);

            sum += result;
        }

        var average = sum / _dataCharArray.Length;
        return average;
    }

    [Benchmark]
    public double AverageOnIntegers()
    {
        long sum = 0;
        string separator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
        Span<char> spanWithoutSeparator = stackalloc char[30];
        const int maxMultiplicationFactor = 10;
        long maxFactorValue = GetFactorValue(maxMultiplicationFactor);
        foreach (ReadOnlyMemory<char> memory in _dataCharArray)
        {
            var span = memory.Span;
            int decimalPosition = span.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
            int spanWithoutSeparatorLength;
            int factor;
            if (decimalPosition == -1)
            {
                span.CopyTo(spanWithoutSeparator);
                spanWithoutSeparatorLength = span.Length;
                factor = maxMultiplicationFactor;
            }
            else
            {
                span.Slice(0, decimalPosition).CopyTo(spanWithoutSeparator);
                span.Slice(decimalPosition + 1).CopyTo(spanWithoutSeparator.Slice(decimalPosition));
                spanWithoutSeparatorLength = span.Length - 1;
                factor = maxMultiplicationFactor - (spanWithoutSeparatorLength - decimalPosition);
            }

            try
            {
                var slicedNumber = spanWithoutSeparator.Slice(0, spanWithoutSeparatorLength);
                int.TryParse(slicedNumber, CultureInfo.InvariantCulture, out int result);
                
                sum += result * GetFactorValue(factor);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to parse span: '{spanWithoutSeparator}' with length {span.Length - 1}", e);
            }
        }
        var average = (double)sum / _dataCharArray.Length / maxFactorValue;
        return average;
    }

    [Benchmark]
    public double AverageOnIntegersWithCustomIndexOf()
    {
        long sum = 0;
        char separator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator[0];
        Span<char> spanWithoutSeparator = stackalloc char[30];
        const int maxMultiplicationFactor = 10;
        long maxFactorValue = GetFactorValue(maxMultiplicationFactor);
        foreach (ReadOnlyMemory<char> memory in _dataCharArray)
        {
            var span = memory.Span;
            int decimalPosition = IndexOf(span, separator);
            int spanWithoutSeparatorLength;
            int factor;
            if (decimalPosition == -1)
            {
                span.CopyTo(spanWithoutSeparator);
                spanWithoutSeparatorLength = span.Length;
                factor = maxMultiplicationFactor;
            }
            else
            {
                span.Slice(0, decimalPosition).CopyTo(spanWithoutSeparator);
                span.Slice(decimalPosition + 1).CopyTo(spanWithoutSeparator.Slice(decimalPosition));
                spanWithoutSeparatorLength = span.Length - 1;
                factor = maxMultiplicationFactor - (spanWithoutSeparatorLength - decimalPosition);
            }

            try
            {
                var slicedNumber = spanWithoutSeparator.Slice(0, spanWithoutSeparatorLength);
                int.TryParse(slicedNumber, CultureInfo.InvariantCulture, out int result);

                sum += result * GetFactorValue(factor);
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to parse span: '{spanWithoutSeparator}' with length {span.Length - 1}", e);
            }
        }
        var average = (double)sum / _dataCharArray.Length / maxFactorValue;
        return average;
    }

    [Benchmark]
    public double AverageOnIntegersWithCustomIntParse()
    {
        long sum = 0;
        char separator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator[0];
        Span<char> spanWithoutSeparator = stackalloc char[30];
        const int maxMultiplicationFactor = 10;
        long maxFactorValue = GetFactorValue(maxMultiplicationFactor);
        foreach (ReadOnlyMemory<char> memory in _dataCharArray)
        {
            var span = memory.Span;
            int decimalPosition = IndexOf(span, separator);
            int spanWithoutSeparatorLength;
            int factor;
            if (decimalPosition == -1)
            {
                span.CopyTo(spanWithoutSeparator);
                spanWithoutSeparatorLength = span.Length;
                factor = maxMultiplicationFactor;
            }
            else
            {
                span.Slice(0, decimalPosition).CopyTo(spanWithoutSeparator);
                span.Slice(decimalPosition + 1).CopyTo(spanWithoutSeparator.Slice(decimalPosition));
                spanWithoutSeparatorLength = span.Length - 1;
                factor = maxMultiplicationFactor - (spanWithoutSeparatorLength - decimalPosition);
            }

            try
            {
                var slicedNumber = spanWithoutSeparator.Slice(0, spanWithoutSeparatorLength);
                int result = 0;
                if (FastTryParseIntOld(slicedNumber, ref result))
                {
                    sum += result * GetFactorValue(factor);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Unable to parse span: '{spanWithoutSeparator}' with length {span.Length - 1}", e);
            }
        }
        var average = (double)sum / _dataCharArray.Length / maxFactorValue;
        return average;
    }

    [Benchmark]
    public double AverageOnIntegersWithCustomMultiplyBy10()
    {
        long sum = 0;
        char separator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator[0];
        Span<char> spanWithoutSeparator = stackalloc char[30];
        const int maxMultiplicationFactor = 10;
        long maxFactorValue = GetFactorValue(maxMultiplicationFactor);
        foreach (ReadOnlyMemory<char> memory in _dataCharArray)
        {
            var span = memory.Span;
            int decimalPosition = IndexOf(span, separator);
            int spanWithoutSeparatorLength;
            int factor;
            if (decimalPosition == -1)
            {
                span.CopyTo(spanWithoutSeparator);
                spanWithoutSeparatorLength = span.Length;
                factor = maxMultiplicationFactor;
            }
            else
            {
                span.Slice(0, decimalPosition).CopyTo(spanWithoutSeparator);
                span.Slice(decimalPosition + 1).CopyTo(spanWithoutSeparator.Slice(decimalPosition));
                spanWithoutSeparatorLength = span.Length - 1;
                factor = maxMultiplicationFactor - (spanWithoutSeparatorLength - decimalPosition);
            }

            var slicedNumber = spanWithoutSeparator.Slice(0, spanWithoutSeparatorLength);
            int result = 0;
            if (FastTryParseIntOld(slicedNumber, ref result))
            {
                //sum += result * GetFactorValue(factor);
                sum += MultiplyByFactor(factor, result);
            }

        }
        var average = (double)sum / _dataCharArray.Length / maxFactorValue;
        return average;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long MultiplyByFactor(int factor, int number)
    {
        return factor switch
        {
            1 => (number << 3) + (number << 1),
            2 => (number << 6) + (number << 5) + (number << 1),
            3 => (number << 9) + (number << 8) + (number << 7) + (number << 6) + (number << 5) + (number << 3),
            4 => number * 10000,
            5 => number * 100000,
            6 => number * 1000000,
            7 => number * 10000000,
            8 => number * 100000000,
            9 => number * 1000000000,
            10 => number * 10000000000,
            11 => number * 100000000000,
            _ => number
        };
    }

    [Benchmark]
    public double AverageOnStringAsIntegers()
    {
        Memory<char> resultString = new Memory<char>(new char[20]);
        Memory<char> tempString = new Memory<char>(new char[20]);
        _dataCharArray[0].CopyTo(tempString.Slice(tempString.Length - _dataCharArray[0].Length));
        int resultSpanPosition = tempString.Length - _dataCharArray[0].Length;
        for (int i = 1; i < _dataCharArray.Length; i++)
        {
            var tempSpan = tempString.Span.Slice(resultSpanPosition);
            resultSpanPosition = SumOfStringNumbers(tempSpan, _dataCharArray[i].Span, resultString.Span);
            resultString.Slice(resultSpanPosition).CopyTo(tempString.Slice(resultSpanPosition));
        }

        double.TryParse(resultString[resultSpanPosition..].Span, CultureInfo.InvariantCulture, out var doubleSum);

        return doubleSum / _dataCharArray.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FastTryParseIntOld(ReadOnlySpan<char> input, ref int result)
    {
        // This check sometimes improves performance - perhaps the extra guarantees enable some JIT optimizations?
        if (input.Length == 0)
        {
            return false;
        }

        var length = input.Length;
        var isNegative = input[0] == '-';
        var offset = isNegative ? 1 : 0;

        // It's faster to not operate directly on 'out' parameters:
        int value = 0;
        for (int i = offset; i < length; i++)
        {
            var c = input[i];
            if (c < '0' || c > '9')
            {
                result = 0;
                return false;
            }

            value = value * 10 + (c - '0');
        }

        // Inputs with 10 digits or more might not fit in an integer, so they'll require additional checks:
        if (length - offset >= 10)
        {
            // Overflow/length checks should ignore leading zeroes:
            var meaningfulDigits = length - offset;
            for (int i = offset; i < length && input[i] == '0'; i++)
                meaningfulDigits -= 1;

            if (meaningfulDigits > 10)
            {
                // Too many digits, this certainly won't fit:
                result = 0;
                return false;
            }
            if (meaningfulDigits == 10)
            {
                // 10-digit numbers can be several times larger than int.MaxValue, so overflow may result in any possible value.
                // However, we only need to check the most significant digit to see if there's a mismatch.
                // Note that int.MinValue always overflows, making it the only case where overflow is allowed:
                if (!isNegative || value != int.MinValue)
                {
                    // Any overflow will cause a leading digit mismatch:
                    if (value / 1000000000 != (input[length - 10] - '0'))
                    {
                        result = 0;
                        return false;
                    }
                }
            }
        }

        // -int.MinValue overflows back into int.MinValue, so that's ok:
        result = isNegative ? -value : value;
        return true;
    }

    private int SumOfStringNumbers(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2, Span<char> resultSpan)
    {
        int decimalSeparator1 = input1.Length - LastIndexOf(input1, '.');
        int decimalSeparator2 = input2.Length - LastIndexOf(input2, '.');

        int skipFromTheEnd1 = 0;
        int skipFromTheEnd2 = 0;
        bool padding1 = false;
        bool padding2 = false;

        if (decimalSeparator1 > decimalSeparator2)
        {
            skipFromTheEnd1 = decimalSeparator1 - decimalSeparator2;
            padding1 = true;
        }
        else if (decimalSeparator2 > decimalSeparator1)
        {
            skipFromTheEnd2 = decimalSeparator2 - decimalSeparator1;
            padding2 = true;
        }

        int resultStringLength = resultSpan.Length;

        int resultSpanPosition = resultStringLength - 1;
        int input1SpanPosition = input1.Length - 1;
        int input2SpanPosition = input2.Length - 1;
        // padding phase is required
        if (decimalSeparator1 != decimalSeparator2)
        {
            if (padding1)
            {
                for (int i = 0; i < skipFromTheEnd1; i++)
                {
                    resultSpan[resultSpanPosition] = input1[input1.Length - 1 - i];
                    resultSpanPosition--;
                    input1SpanPosition--;
                }
            }
            else if (padding2)
            {
                for (int i = 0; i < skipFromTheEnd2; i++)
                {
                    resultSpan[resultSpanPosition] = input2[input2.Length - 1 - i];
                    resultSpanPosition--;
                    input2SpanPosition--;
                }
            }
        }

        bool firstNegative = input1[0] == '-';
        bool secondNegative = input2[0] == '-';
        // summing
        if (firstNegative == secondNegative) // both negative or both positive
        {
            int overflow = 0;
            for (; resultSpanPosition >= 0; resultSpanPosition--)
            {
                char firstCharacter = input1SpanPosition >= 0 ? input1[input1SpanPosition] : '0';
                char secondCharacter = input2SpanPosition >= 0 ? input2[input2SpanPosition] : '0';
                if (firstCharacter != '.')
                {
                    int numberSum = firstCharacter + secondCharacter - 96 + overflow;

                    if (numberSum > 9)
                    {
                        overflow = 1;
                        numberSum -= 10;
                    }
                    else
                    {
                        overflow = 0;
                    }

                    resultSpan[resultSpanPosition] = (char)(numberSum + 48);
                }
                else
                {
                    resultSpan[resultSpanPosition] = '.';
                }

                input1SpanPosition--;
                input2SpanPosition--;

                if (input1SpanPosition < 0 && input2SpanPosition < 0)
                {
                    if (overflow > 0)
                    {
                        resultSpanPosition--;
                        resultSpan[resultSpanPosition] = '1';
                    }
                    break;
                }
            }
        }

        return resultSpanPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IndexOf(in ReadOnlySpan<char> span, char valueToFind)
    {
        for (var spanIndex = 0; spanIndex < span.Length; spanIndex++)
        {
            if (span[spanIndex] == valueToFind)
            {
                return spanIndex;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int LastIndexOf(ReadOnlySpan<char> span, char valueToFind)
    {
        for (var spanIndex = span.Length - 1; spanIndex >= 0; spanIndex--)
        {
            if (span[spanIndex] == valueToFind)
            {
                return spanIndex;
            }
        }

        return -1;
    }

    /// <summary>
    /// Use switch statement to avoid extra multiplication on a hot path.
    /// </summary>
    /// <param name="factor">Power of ten required.</param>
    /// <returns>Power of 10 number corresponding <paramref name="factor"/></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetFactorValue(int factor)
    {
        return factor switch
        {
            1 => 10,
            2 => 100,
            3 => 1000,
            4 => 10000,
            5 => 100000,
            6 => 1000000,
            7 => 10000000,
            8 => 100000000,
            9 => 1000000000,
            10 => 10000000000,
            11 => 100000000000,
            _ => 1
        };
    }
}