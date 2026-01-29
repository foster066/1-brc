using Runner.Base;
using System.IO.MemoryMappedFiles;
using System.Text;
using Runner.ReadTest;

namespace Runner.Implementations;

public class MemoryMappedFileTest : BaseRunner
{
    private const int readChunk = Constants.ReadBufferSize;
    public override void Run(string fileName)
    {
        byte[] buffer = new byte[readChunk];
        char[] charBuffer = new char[readChunk];
        long fileIndex = 0;
        int unfinishedBufferSize = 0;
        using var streamReader = MemoryMappedFile.CreateFromFile(fileName);
        var reader = streamReader.CreateViewAccessor();

        char[] newBuffer = new char[readChunk];
        Span<char> newSpanBuffer = newBuffer.AsSpan();

        while (fileIndex < reader.Capacity)
        {
            int readBytes = reader.ReadArray(fileIndex, buffer, unfinishedBufferSize, buffer.Length - unfinishedBufferSize);
            if (readBytes == 0)
            {
                break;
            }
            fileIndex += readBytes;

            var spanBufferSize = Encoding.UTF8.GetChars(buffer.AsSpan(0, readBytes + unfinishedBufferSize), charBuffer.AsSpan());
            var spanBuffer = charBuffer.AsSpan(0, spanBufferSize);

            spanBuffer.Slice(spanBufferSize).CopyTo(newSpanBuffer);
        }
    }
}