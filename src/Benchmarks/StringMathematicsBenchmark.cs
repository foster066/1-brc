using System;
using System.Globalization;
using BenchmarkDotNet.Attributes;

namespace Benchmarks;

public class StringMathematicsBenchmark
{
    [Benchmark]
    public void StringSum()
    {
        ReadOnlySpan<char> input1 = "1231231.231231233";
        ReadOnlySpan<char> input2 = "32324234333.567877";
        int resultStringLength = Math.Max(input1.Length, input2.Length) + 40;
        Memory<char> resultString = new Memory<char>(new char[resultStringLength]);

        int resultStringStartPosition = SumOfStringNumbers(input1, input2, resultString.Span);
        var resultSpan = resultString.Span.Slice(resultStringStartPosition);

        bool parseResult = double.TryParse(resultSpan, CultureInfo.InvariantCulture, out var doubleValue);
    }

    public int SumOfStringNumbers(ReadOnlySpan<char> input1, ReadOnlySpan<char> input2, Span<char> resultSpan)
    {
        int decimalSeparator1 = input1.Length - input1.IndexOf('.');
        int decimalSeparator2 = input2.Length - input2.IndexOf('.');

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
        bool secondNegative = input2[2] == '-';
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
                    break;
                }
            }
        }

        return resultSpanPosition;
    }
}