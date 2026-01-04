namespace Runner.Base;

public abstract class BaseRunner
{
    public abstract void Run(string fileName);

    public required string TempFolder { get; set; }
    public required string ResultFile { get; set; }
}