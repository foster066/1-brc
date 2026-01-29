using Runner.Base;
using System.Diagnostics;
using System.Globalization;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace Runner.Implementations;

public class Implementation2 : BaseRunner
{
    private const int readChunk = 4 * 1024;
    private static readonly char _decimalSeparator = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator[0];
    private static readonly string _resultFile = "./result.txt";
    private readonly long _maxFactorValue;
    private const int _maxMultiplicationFactor = 4;
    private static readonly ReadOnlyMemory<char> _symbolsToFindForString = new([';', '\n']);
    private readonly Memory<int> _searchIndexes = new([-1, -1]);

    public Implementation2()
    {
        ResultFile = _resultFile;
        
        _maxFactorValue = GetFactorValue(_maxMultiplicationFactor);
    }

    public override void Run(string fileName)
    {
         // Phase 1 read and fill stats for stations
         Stopwatch stopwatch = Stopwatch.StartNew();

         Dictionary<string, StationData> measurements = ReadInputDataFile(fileName);
         Log($"All read {measurements.Count} stations in {stopwatch.Elapsed}");
         stopwatch.Restart();

        // Phase 2 write results
        WriteResultFile(measurements);
        Log($"All results are written in {stopwatch.ElapsedMilliseconds} ms");
    }

    private void WriteResultFile(Dictionary<string, StationData> measurements)
    {
        using var writer = new StreamWriter(File.OpenWrite(ResultFile));

        foreach (var measurement in new OrderedDictionary<string, StationData>(measurements))
        {
            var average = Math.Round((double)measurement.Value.Sum / measurement.Value.Count / _maxFactorValue, 4, MidpointRounding.AwayFromZero);
            var max = Math.Round((double)measurement.Value.Max / _maxFactorValue, 4, MidpointRounding.AwayFromZero);
            var min = Math.Round((double)measurement.Value.Min / _maxFactorValue, 4, MidpointRounding.AwayFromZero);
            
            writer.WriteLine($"{measurement.Key};{average};{min};{max}");
        }
    }

    private Dictionary<string, StationData> ReadInputDataFile(string fileName)
    {
        Dictionary<string, StationData> measurements = new(500);
        Dictionary<string, StationData>.AlternateLookup<ReadOnlySpan<char>> alternateLookup = measurements.GetAlternateLookup<ReadOnlySpan<char>>();

        byte[] buffer = new byte[readChunk];
        char[] charBuffer = new char[readChunk];
        byte[] unfinishedBuffer = new byte[1024];
        
        Span<byte> unfinishedBufferSpan = unfinishedBuffer.AsSpan();

        using var streamReader = MemoryMappedFile.CreateFromFile(fileName);
        var reader = streamReader.CreateViewAccessor();
        int iterationIndex = 0;
        long fileIndex = 0;
        int readIterations = 0;
        int unfinishedBufferSize = 0;
        var fileLength = new FileInfo(fileName).Length;
        Span<char> spanBufferForIntegerParsing = stackalloc char[30]; // allocation at the beginning to avoid stack allocation in a loop
        
        while (fileIndex < reader.Capacity)
        {
            if (unfinishedBufferSize > 0)
            {
                unfinishedBufferSpan.Slice(0, unfinishedBufferSize).CopyTo(buffer);
            }

            int readBytes = reader.ReadArray(fileIndex, buffer, unfinishedBufferSize, buffer.Length - unfinishedBufferSize);
            if (readBytes == 0)
            {
                break;
            }
            fileIndex += readBytes;
            
            var spanBufferSize = Encoding.UTF8.GetChars(buffer.AsSpan(0, readBytes + unfinishedBufferSize), charBuffer.AsSpan());
            var spanBuffer = charBuffer.AsSpan(0, spanBufferSize);

            do
            {
                if (fileIndex + iterationIndex >= fileLength)
                {
                    unfinishedBufferSize = 0;
                    iterationIndex = 0;
                    break;
                }

                // return a measurement consisted from station name and its value
                if (!GetMeasurementString(spanBuffer, spanBufferSize, ref iterationIndex, out var stationName, out var value))
                {
                    // if we hit the end of a buffer before a measurement string ends 
                    unfinishedBufferSize = spanBufferSize - iterationIndex;
                    if (unfinishedBufferSize > 0 && fileIndex + iterationIndex <= fileLength)
                    {
                        bool unfinishedSymbol = charBuffer[spanBufferSize - 1] == '\xFFFD';
                        if (unfinishedSymbol)
                        {
                            unfinishedBufferSize--;
                        }

                        // case when buffer ends with wide character cut-off and there is any valid character
                        if (unfinishedBufferSize > 0)
                        {
                            unfinishedBufferSize = Encoding.UTF8.GetByteCount(spanBuffer.Slice(iterationIndex, unfinishedBufferSize));//.CopyTo(unfinishedBuffer.AsSpan());

                            if (unfinishedSymbol)
                            {
                                unfinishedBufferSize++;
                            }
                            buffer.AsSpan(buffer.Length - unfinishedBufferSize).CopyTo(unfinishedBufferSpan);
                        }
                        // case when the only left over in previous buffer is cut-off wide character
                        else if (unfinishedSymbol)
                        {
                            unfinishedBuffer[unfinishedBufferSize] = buffer[buffer.Length - 1];
                            unfinishedBufferSize++;
                        }
                    }

                    iterationIndex = 0;
                    break;
                }

                if (!alternateLookup.TryGetValue(stationName, out var stationData))
                {
                    stationData = new();
                    measurements.Add(stationName.ToString(), stationData);
                }

                long integerValue = 0;
                if (ParseIntegerValueFromString(value, spanBufferForIntegerParsing, ref integerValue))
                {
                    CalculateStationsStats(stationData, integerValue);
                }
                else
                {
                    Log("Cannot parse value to int: " + value.ToString());
                }
                
            } while (true);
            

            readIterations++;
            if (readIterations % 250_000_000 == 0)
            {
                Log($"Read {(int)((double)fileIndex / fileLength * 100)}% ...");
            }
        }

        return measurements;
    }

    private bool ParseIntegerValueFromString(Span<char> span, Span<char> spanBufferForIntegerParsing, ref long result)
    {
        int decimalPosition = IndexOf(span, _decimalSeparator);
        int spanWithoutSeparatorLength;
        int factor;
        if (decimalPosition == -1)
        {
            span.CopyTo(spanBufferForIntegerParsing);
            spanWithoutSeparatorLength = span.Length;
            factor = _maxMultiplicationFactor;
        }
        else
        {
            span.Slice(0, decimalPosition).CopyTo(spanBufferForIntegerParsing);
            span.Slice(decimalPosition + 1).CopyTo(spanBufferForIntegerParsing.Slice(decimalPosition));
            spanWithoutSeparatorLength = span.Length - 1;
            factor = _maxMultiplicationFactor - (spanWithoutSeparatorLength - decimalPosition);
        }

        var slicedNumber = spanBufferForIntegerParsing.Slice(0, spanWithoutSeparatorLength);
        int intValue = 0;
        if (TryParseInt(slicedNumber, ref intValue))
        {
            result = MultiplyByFactorIf(factor, intValue);
            return true;
        }
        return false;
    }

    private bool TryParseInt(Span<char> input, ref int result)
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

    private void CalculateStationsStats(StationData stationData, long value)
    {
        if (value > stationData.Max || stationData.Count == 0)
        {
            stationData.Max = value;
        }

        if (stationData.Min > value || stationData.Count == 0)
        {
            stationData.Min = value;
        }

        stationData.Sum += value;
        stationData.Count++;
    }

    private void Log(string msg)
    {
        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss:ffff ") + msg);
    }

    private bool GetMeasurementString(Span<char> spanBuffer, int spanBufferSize, ref int index, out Span<char> stationName,
        out Span<char> value)
    {
        var searchSpan = spanBuffer.Slice(index);
        int originalIndex = index;

        _searchIndexes.Span[0] = -1;
        _searchIndexes.Span[1] = -1;
        IndexOfAll(searchSpan, _symbolsToFindForString, _searchIndexes.Span);
        int separatorIndex = _searchIndexes.Span[0];
        int indexEnd = _searchIndexes.Span[1];
        if (indexEnd == -1 || index >= spanBufferSize || index + separatorIndex >= spanBufferSize || index + indexEnd >= spanBufferSize)
        {
            stationName = Span<char>.Empty;
            value = Span<char>.Empty;
            return false;
        }

        stationName = searchSpan.Slice(0, separatorIndex);
        separatorIndex++;

        value = searchSpan.Slice(separatorIndex, indexEnd - separatorIndex);
        index = originalIndex + indexEnd + 1;
        return true;
    }

    private class StationData
    {
        public long Min { get; set; }
        public long Max { get; set; }
        public int Count { get; set; }
        public long Sum { get; set; }
    }

    /// <summary>
    /// Use switch statement to avoid extra multiplication on a hot path.
    /// </summary>
    /// <param name="factor">Power of ten required.</param>
    /// <returns>Power of 10 number corresponding <paramref name="factor"/></returns>
    private long GetFactorValue(long factor)
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

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void IndexOfAll<T>(Span<T> span, ReadOnlyMemory<T> valuesToFind, Span<int> results)
    {
        long resultFlag = 1 << results.Length;
        for (var spanIndex = 0; spanIndex < span.Length; spanIndex++)
        {
            for (int i = 0; i < valuesToFind.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(span[spanIndex], valuesToFind.Span[i]))
                {
                    results[i] = spanIndex;
                    resultFlag >>= 1;

                    if (resultFlag == 1)
                    {
                        return;
                    }
                }
            }
        }
    }

    private long MultiplyByFactorIf(int factor, int number)
    {
        if (factor == 1)
            return (number << 3) + (number << 1);
        if (factor == 2)
            return (number << 6) + (number << 5) + (number << 1);
        if (factor == 3)
            return (number << 9) + (number << 8) + (number << 7) + (number << 6) + (number << 5) + (number << 3);
        if (factor == 4)
            return number * 10000;
        if (factor == 5)
            return number * 100000;
        if (factor == 6)
            return number * 1000000;
        if (factor == 7)
            return number * 10000000;
        if (factor == 8)
            return number * 100000000;
        if (factor == 9)
            return number * 1000000000;
        if (factor == 10)
            return number * 10000000000;
        if (factor == 11)
            return number * 100000000000;
        return number;
    }
}