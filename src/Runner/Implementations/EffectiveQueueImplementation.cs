using Runner.Base;
using Runner.ReadTest;

namespace Runner.Implementations;

public class EffectiveQueueImplementation : BaseRunner
{
    private const int segmentSize = Constants.ReadBufferSize;
    private readonly int processingThreadsCount = Environment.ProcessorCount - 1;

    public override void Run(string fileName)
    {
        
    }

    class MeasurementValue
    {
        public Memory<char> Station { get; set; }
        public Memory<char> Value { get; set; }
    }
}