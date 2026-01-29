using Runner.Base;
using Runner.ReadTest;

namespace Runner.Implementations;

public class StreamReaderTest : BaseRunner
{
    private const int readChunk = Constants.ReadBufferSize;
    public override void Run(string fileName)
    {
        using StreamReader streamReader = new StreamReader(fileName, null, false);
        char[] buffer = new char[readChunk];
        Span<char> spanBuffer = buffer.AsSpan();

        bool isContinue = true;
        char[] newBuffer = new char[readChunk];
        Span<char> newSpanBuffer = newBuffer.AsSpan();
        while (isContinue)
        {
            int readSymbols = streamReader.ReadBlock(buffer, 0, buffer.Length);

            if (readSymbols < readChunk)
            {
                isContinue = false;
                break;
            }

            //spanBuffer.Slice(readSymbols).CopyTo(newSpanBuffer);
        }
    }
}