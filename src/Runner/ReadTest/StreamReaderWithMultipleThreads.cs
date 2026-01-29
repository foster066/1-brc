using Runner.Base;
using Runner.ReadTest;
using System.Collections.Concurrent;
using System.IO;

namespace Runner.Implementations;

public class StreamReaderWithMultipleThreads : BaseRunner
{
    private const int readChunk = Constants.ReadBufferSize;
    public override void Run(string fileName)
    {
        const int threadsCount = 2;
        FileInfo fileInfo = new FileInfo(fileName);

        Parallel.ForEach(Partitioner.Create(0, fileInfo.Length, fileInfo.Length / 2),
            new ParallelOptions { MaxDegreeOfParallelism = threadsCount },
            (range, stop) =>
            {
                using StreamReader streamReader = new StreamReader(fileName, null, false);
                char[] buffer = new char[readChunk];
                Span<char> spanBuffer = buffer.AsSpan();

                bool isContinue = true;
                char[] newBuffer = new char[readChunk];
                Span<char> newSpanBuffer = newBuffer.AsSpan();
                streamReader.BaseStream.Position = range.Item1;
                while (streamReader.BaseStream.Position < range.Item2)
                {
                    int readSymbols = streamReader.ReadBlock(buffer, 0, buffer.Length);

                    if (readSymbols < readChunk)
                    {
                        //isContinue = false;
                        break;
                    }

                    //spanBuffer.Slice(readSymbols).CopyTo(newSpanBuffer);
                }
            });

    }
}