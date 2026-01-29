using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;
using System.Globalization;
using System.Text;

namespace Benchmarks;

[CPUUsageDiagnoser]
[MemoryDiagnoser]
public class FloatConversionBenchmark
{
    private ReadOnlyMemory<char> _dataChar;
    private ReadOnlyMemory<byte> _dataByte;

    [IterationSetup]
    public void Setup()
    {
        Random rnd = new();
        _dataChar = rnd.NextSingle().ToString(CultureInfo.InvariantCulture).AsMemory();
        _dataByte = Encoding.UTF8.GetBytes(rnd.NextSingle().ToString(CultureInfo.InvariantCulture));
    }

    [Benchmark]
    public float TryParseFloatFromCharSpan()
    {
        float.TryParse(_dataChar.Span, CultureInfo.InvariantCulture, out var value);

        return value;
    }

    [Benchmark]
    public float ParseFloatFromCharSpan()
    {
        return float.Parse(_dataChar.Span, CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public float TryParseFloatFromByteSpan()
    {
        float.TryParse(_dataByte.Span, CultureInfo.InvariantCulture, out var value);

        return value;
    }

    [Benchmark]
    public float ParseFloatFromByteSpan()
    {
        return float.Parse(_dataByte.Span, CultureInfo.InvariantCulture);
    }
}