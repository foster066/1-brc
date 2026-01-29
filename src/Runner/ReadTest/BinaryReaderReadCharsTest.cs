using Runner.Base;
using Runner.ReadTest;

namespace Runner.Implementations;

public class BinaryReaderReadCharsTest : BaseRunner
{
    private const int readChunk = Constants.ReadBufferSize;
    public override void Run(string fileName)
    {
        Span<char> spanBuffer = new char[readChunk];
        long fileIndex = 0;
        using var stream = File.OpenRead(fileName);
        using var reader = new BinaryReader(stream);

        char[] newBuffer = new char[readChunk];
        Span<char> newSpanBuffer = newBuffer.AsSpan();

        while (fileIndex < stream.Length)
        {
            int readBytes = reader.Read(spanBuffer);
            if (readBytes == 0)
            {
                break;
            }
            fileIndex += readBytes;

            spanBuffer.Slice(readBytes).CopyTo(newSpanBuffer);
        }
    }
}