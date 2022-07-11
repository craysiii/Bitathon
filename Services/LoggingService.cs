using System.Runtime.CompilerServices;

namespace Bitathon.Services;

// Logging is complicated currently so we implement our own rudimentary logger
public class LoggingService
{
    private Queue<string> LogQueue { get; } = new();
    private PeriodicTimer PeriodicTimer { get; } = new(TimeSpan.FromMilliseconds(50));
    private CancellationToken TimerCancellationToken { get; } = new();

    private const string LogPath = "bitathon.log";

    public LoggingService()
    {
        ProcessLogQueue();
    }

    private async Task ProcessLogQueue()
    {
        while (await PeriodicTimer.WaitForNextTickAsync(TimerCancellationToken) &&
               !TimerCancellationToken.IsCancellationRequested)
        {
            if (LogQueue.Count <= 0) continue;
            await using var streamWriter = File.AppendText(LogPath);
            await streamWriter.WriteLineAsync(LogQueue.Dequeue());
        }
    }

    public void Log(string message, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFilePath = "")
    {
        var filePath = Path.GetFileName(callerFilePath);
        LogQueue.Enqueue($"[{DateTime.Now.ToString(format: "O")}] {filePath}#{callerName}: {message}");
    }
    
    
}