using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using Runner.Base;

namespace Runner.Implementations;

public class Implementation1 : BaseRunner
{
    private const int readChunk = 16 * 1024;
    private static readonly ReadOnlyMemory<char> _separatorChar = new([';']);
    public static string _temporaryFolder = "./temp";
    public static string _resultFile = "./result.txt";

    public Implementation1()
    {
        TempFolder = _temporaryFolder;
        ResultFile = _resultFile;
    }

    public override void Run(string fileName)
    {
        // Phase 1
        // Read the source data file and put measurements into a corresponding station file
        Directory.CreateDirectory(_temporaryFolder);
        // Read a block from the data file and then parse if line by line
        Stopwatch stopwatch = Stopwatch.StartNew();
        Dictionary<string, StationData> measurements = ReadInputDataFile(fileName);

        Log($"All read {measurements.Count} stations in {stopwatch.Elapsed}");
        stopwatch.Restart();
        FlushAllStreams(measurements);

        Log($"All Flushed in {stopwatch.Elapsed}, Start calculations...");
        stopwatch.Restart();
        // Phase 2
        // Calculate statistics per station
        CalculateStatistics(measurements);
        Log($"Stats are calculated in {stopwatch.Elapsed} for {measurements.Count} stations");
        stopwatch.Restart();

        // Phase 3
        // Write result file
        WriteResultFile(measurements);
        Log($"All results are written in {stopwatch.Elapsed}");
        stopwatch.Restart();
        CloseAllStreams(measurements);
        Log($"All files are closed in {stopwatch.Elapsed}");
    }

    private static void CloseAllStreams(Dictionary<string, StationData> measurements)
    {
        foreach (var measurement in measurements)
        {
            measurement.Value.StreamWriter.Close();
        }
    }

    private static void FlushAllStreams(Dictionary<string, StationData> measurements)
    {
        foreach (var measurement in measurements)
        {
            measurement.Value.StreamWriter.Flush();
        }
    }

    private Dictionary<string, StationData> ReadInputDataFile(string fileName)
    {
        Dictionary<string, StationData> measurements = new(500);
        using StreamReader streamReader = new StreamReader(fileName);
        char[] buffer = new char[readChunk];
        Span<char> spanBuffer = buffer.AsSpan();
        int unfinishedBufferSize = 0;
        char[] unfinishedBuffer = new char[1024];
        
        int index = 0;
        int stationIndex = 0;
        int readIterations = 0;
        bool isContinue = true;
        //Stopwatch sw = Stopwatch.StartNew();
        while (isContinue)
        {
            if (unfinishedBufferSize > 0)
            {
                unfinishedBuffer.AsSpan(0, unfinishedBufferSize).CopyTo(spanBuffer);
            }
            int readSymbols = streamReader.ReadBlock(buffer, unfinishedBufferSize, buffer.Length - unfinishedBufferSize);

            if (readSymbols == 0)
            {
                isContinue = false;
            }

            do
            {
                if (streamReader.EndOfStream && unfinishedBufferSize + readSymbols == index || readSymbols == 0)
                {
                    unfinishedBufferSize = 0;
                    index = 0;
                    break;
                }
                // return a measurement consisted from station name and its value
                if (!GetMeasurementString(spanBuffer, ref index, out string stationName, out var value))
                {
                    // if we hit the end of a buffer before a measurement string ends 
                    unfinishedBufferSize = buffer.Length - index;
                    if (unfinishedBufferSize > 0)
                    {
                        spanBuffer.Slice(index, unfinishedBufferSize).CopyTo(unfinishedBuffer.AsSpan());
                    }

                    index = 0;
                    break;
                }

                if (!measurements.TryGetValue(stationName, out var stationData))
                {
                    stationData = new StationData(new StreamWriter(File.Open(Path.Combine(_temporaryFolder, (++stationIndex).ToString()), FileMode.CreateNew, FileAccess.ReadWrite)));
                    measurements.Add(stationName, stationData);
                }

                if (value.Length > stationData.Length)
                {
                    stationData.Length = value.Length;
                }

                stationData.StreamWriter.WriteLine(value);
                
                
            } while (true);

            readIterations++;
            //Log($"Read file iteration {readIterations} took {sw.Elapsed.TotalMicroseconds:N0} mks");
            //sw.Restart();
            if (readIterations % 100_000 == 0)
            {
                Log($"Read {(int)((double)streamReader.BaseStream.Position / streamReader.BaseStream.Length * 100)}% ...");
            }
        }

        return measurements;
    }

    private void WriteResultFile(Dictionary<string, StationData> measurements)
    {
        using var writer = TextWriter.Synchronized(new StreamWriter(File.OpenWrite(ResultFile)));

        foreach (var measurement in new OrderedDictionary<string, StationData>(measurements))
        {
            writer.WriteLine($"{measurement.Key};{measurement.Value.Average};{measurement.Value.Min};{measurement.Value.Max}");
        }
    }

    private void CalculateStatistics(Dictionary<string, StationData> measurements)
    {
        int index = 1;
        foreach (var data in measurements)
        {
            List<double> measurementsList = GetMeasurementsFromFileReadBlock(data, index);
            double min = double.MaxValue;
            double max = double.MinValue;
            double sum = 0;
            foreach (var value in measurementsList)
            {
                if (value > max)
                {
                    max = value;
                }

                if (value < min)
                {
                    min = value;
                }
                sum += value;
            }

            data.Value.Min = min;
            data.Value.Max = max;
            data.Value.Average = Math.Round(sum / measurementsList.Count, 4, MidpointRounding.AwayFromZero);
            index++;
        }
    }

    private List<double> GetMeasurementsFromFileReadBlock(KeyValuePair<string, StationData> data, int fileIndex)
    {
        var estimatedMeasurementsSize = GetEstimatedCount(data.Value.StreamWriter.BaseStream.Position, data.Value.Length);
        List<double> measurementsList = new(estimatedMeasurementsSize);
        data.Value.StreamWriter.BaseStream.Position = 0;
        StreamReader reader = new(data.Value.StreamWriter.BaseStream);
        const int blockSize = 16 * 1024;
        char[] buffer = ArrayPool<char>.Shared.Rent(blockSize);
        int index = 0;
        char[] unfinishedBuffer = ArrayPool<char>.Shared.Rent(100);
        int unfinishedBufferSize = 0;
        Span<char> spanBuffer = buffer.AsSpan();
        //Stopwatch sw = Stopwatch.StartNew();
        do
        {
            if (unfinishedBufferSize > 0)
            {
                unfinishedBuffer.AsSpan(0, unfinishedBufferSize).CopyTo(spanBuffer);
            }
            int readBytes = reader.ReadBlock(buffer, unfinishedBufferSize, blockSize - unfinishedBufferSize);
            if (readBytes > 0)
            {
                readBytes += unfinishedBufferSize;
            }
            while (readBytes > 0)
            {
                try
                {
                    if (index >= readBytes)
                    {
                        unfinishedBufferSize = 0;
                        index = 0;
                        break;
                    }
                    var searchSpan = spanBuffer.Slice(index);
                    var endIndex = searchSpan.IndexOf('\r');
                    if (endIndex == -1)
                    {
                        if (searchSpan.Length > 0)
                        {
                            searchSpan.CopyTo(unfinishedBuffer);
                        }
                        
                        unfinishedBufferSize = searchSpan.Length;
                        index = 0;
                        break;
                    }

                    if (double.TryParse(searchSpan.Slice(0, endIndex), CultureInfo.InvariantCulture, out double value))
                    {
                        measurementsList.Add(value);
                    }
                    else
                    {
                        Log("Cannot parse double value " + searchSpan.Slice(0, endIndex).ToString());
                    }

                    index += endIndex + 2;
                }
                catch (Exception e)
                {
                    Log($"Cannot process line file {fileIndex} with index {index}, error {e}");
                    throw;
                }
            }
        } while (!reader.EndOfStream);

        ArrayPool<char>.Shared.Return(unfinishedBuffer);
        ArrayPool<char>.Shared.Return(buffer);

        //Log($"File {fileIndex} processed in {sw.Elapsed.TotalMilliseconds} ms");

        return measurementsList;
    }

    private int GetEstimatedCount(long fileSize, int valueLength)
    {
        return Convert.ToInt32(fileSize / valueLength);
    }

    private bool GetMeasurementString(Span<char> spanBuffer, ref int index, out string stationName, out Span<char> value)
    {
        var searchSpan = spanBuffer.Slice(index);
        int originalIndex = index;
        int separatorIndex = searchSpan.IndexOf(_separatorChar.Span, StringComparison.OrdinalIgnoreCase);
        int indexEnd = searchSpan.IndexOf('\n');
        if (indexEnd == -1)
        {
            stationName = string.Empty;
            value = Span<char>.Empty;
            return false;
        }

        stationName = searchSpan.Slice(0, separatorIndex).ToString();
        separatorIndex++;

        value = searchSpan.Slice(separatorIndex, indexEnd - separatorIndex);
        index = originalIndex + indexEnd + 1;
        return true;
    }

    private void Log(string msg)
    {
        Console.WriteLine(DateTime.Now.ToString("HH:mm:ss:ffff ") + msg);
    }
}

public class StationData
{
    public StationData(StreamWriter streamWriter)
    {
        StreamWriter = streamWriter;
    }
    public StreamWriter StreamWriter { get; set; }
    public double Average { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int Length { get; set; }
}