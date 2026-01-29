using Runner.Base;
using Runner.ReadTest;
using System;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace Runner.Implementations;

public class MemoryMappedFileMultipleThreads : BaseRunner
{
    private const int readChunk = Constants.ReadBufferSize;
    public override void Run(string fileName)
    {
        byte[] buffer = new byte[readChunk];
        char[] charBuffer = new char[readChunk];
        
        int unfinishedBufferSize = 0;
        var streamReader = MemoryMappedFile.CreateFromFile(fileName);
        //var reader = streamReader.CreateViewAccessor();
        FileInfo fileInfo = new FileInfo(fileName);
        char[] newBuffer = new char[readChunk];
        Span<char> newSpanBuffer = newBuffer.AsSpan();
        const int threadsCount = 2;

        Parallel.ForEach(Partitioner.Create(0, fileInfo.Length, fileInfo.Length / 2), new ParallelOptions { MaxDegreeOfParallelism = threadsCount }, (range, stop) =>
        {
            var reader = streamReader.CreateViewAccessor(range.Item1, range.Item2 - range.Item1);
            long fileIndex = 0;
            while (fileIndex < reader.Capacity)
            {
                int readBytes = reader.ReadArray(fileIndex, buffer, unfinishedBufferSize, buffer.Length - unfinishedBufferSize);
                if (readBytes == 0)
                {
                    break;
                }
                fileIndex += readBytes;

                var spanBufferSize = Encoding.UTF8.GetChars(buffer.AsSpan(0, readBytes + unfinishedBufferSize), charBuffer.AsSpan());
                //var spanBuffer = charBuffer.AsSpan(0, spanBufferSize);

                //spanBuffer.Slice(spanBufferSize).CopyTo(newSpanBuffer);
            }
        });

        
    }
}