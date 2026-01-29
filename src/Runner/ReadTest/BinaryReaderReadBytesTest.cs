using Runner.Base;
using System.Text;
using Runner.ReadTest;

namespace Runner.Implementations;

public class BinaryReaderReadBytesTest : BaseRunner
{
    private const int readChunk = Constants.ReadBufferSize;
    public override void Run(string fileName)
    {
        byte[] buffer = new byte[readChunk];
        Span<byte> spanByteBuffer = buffer.AsSpan();
        char[] charBuffer = new char[readChunk];
        Span<char> spanCharBuffer = charBuffer.AsSpan();
        long fileIndex = 0;
        int unfinishedBufferSize = 0;
        using var stream = File.OpenRead(fileName);
        using var reader = new BinaryReader(stream);

        char[] newBuffer = new char[readChunk];
        Span<char> newSpanBuffer = newBuffer.AsSpan();

        while (fileIndex < stream.Length)
        {
            int readBytes = reader.Read(spanByteBuffer);
            if (readBytes == 0)
            {
                break;
            }
            fileIndex += readBytes;

            var spanBufferSize = Encoding.UTF8.GetChars(spanByteBuffer.Slice(0, readBytes + unfinishedBufferSize), spanCharBuffer);
            var spanBuffer = spanCharBuffer.Slice(0, spanBufferSize);

            spanBuffer.Slice(spanBufferSize).CopyTo(newSpanBuffer);
        }
    }
}