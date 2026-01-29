using Runner;
using Runner.Base;
using System.Diagnostics;
using System.Reflection;

string implementation = "Implementation1";
string fileName = "./data/weather_stations.csv";

if (args.Length > 0)
{
    fileName = args[0];
}
if (args.Length > 1)
{
    implementation = args[1];
}

PowerManager powerManager = new PowerManager();
var currentPlan = powerManager.GetCurrentPlan();
powerManager.SetActive(powerManager.MaximumPerformance);

string implementationName = "Runner.Implementations." + implementation;

if (Activator.CreateInstance(Assembly.GetCallingAssembly().FullName!, implementationName)?.Unwrap() is BaseRunner runner)
{
    Console.WriteLine($"Running implementation '{implementationName}'");
    CleanTempDirectory();

    Stopwatch sw = Stopwatch.StartNew();
    runner.Run(fileName);
    sw.Stop();
    Console.WriteLine($"Implementation {implementation} run took: {sw.Elapsed} or {sw.Elapsed.TotalMilliseconds} ms");
}
else
{
    Console.WriteLine($"Implementation '{implementationName}' not found");
}

powerManager.SetActive(currentPlan);

void CleanTempDirectory()
{
    if (Directory.Exists(runner.TempFolder))
    {
        DirectoryInfo directory = new(runner.TempFolder);
        int fileCount = 0;
        foreach (FileInfo file in directory.EnumerateFiles())
        {
            file.Delete();
            fileCount++;
        }

        Console.WriteLine($"Cleaned {fileCount} temp files");
    }
}