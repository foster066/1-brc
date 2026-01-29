using BenchmarkDotNet.Attributes;
using Microsoft.VSDiagnostics;
using System;
using System.Globalization;
using System.Text;

namespace Benchmarks;

[CPUUsageDiagnoser]
[MemoryDiagnoser]
public class DoubleConversionBenchmark
{
    private ReadOnlyMemory<char> _dataChar;
    private ReadOnlyMemory<byte> _dataByte;

    [IterationSetup]
    public void Setup()
    {
        Random rnd = new();
        _dataChar = rnd.NextDouble().ToString(CultureInfo.InvariantCulture).AsMemory();
        _dataByte = Encoding.UTF8.GetBytes(rnd.NextDouble().ToString(CultureInfo.InvariantCulture));
    }

    [Benchmark]
    public double TryParseDoubleFromCharSpan()
    {
        double.TryParse(_dataChar.Span, CultureInfo.InvariantCulture, out var value);

        return value;
    }

    [Benchmark]
    public double ParseDoubleFromCharSpan()
    {
        return double.Parse(_dataChar.Span, CultureInfo.InvariantCulture);
    }

    [Benchmark]
    public double TryParseDoubleFromByteSpan()
    {
        double.TryParse(_dataByte.Span, CultureInfo.InvariantCulture, out var value);

        return value;
    }

    [Benchmark]
    public double ParseDoubleFromByteSpan()
    {
        return double.Parse(_dataByte.Span, CultureInfo.InvariantCulture);
    }
}