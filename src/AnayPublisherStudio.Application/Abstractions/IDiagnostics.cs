namespace AnayPublisherStudio.Application.Abstractions;

public interface IDeveloperConsoleService
{
    event Action<string>? MessageLogged;
    IReadOnlyList<string> Messages { get; }
    void Log(string message);
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void Clear();
}

public interface IPerformanceMonitor
{
    event Action<string, double>? MetricRecorded;
    IDisposable BeginOperation(string name);
    void RecordMetric(string name, double milliseconds);
    IReadOnlyDictionary<string, double> Snapshot { get; }
    void Reset();
}

public interface IMemoryMonitor
{
    event Action<MemoryMetrics>? MetricsUpdated;
    MemoryMetrics Current { get; }
    void Start(TimeSpan interval);
    void Stop();
}

public sealed record MemoryMetrics
{
    public long ManagedMemoryBytes { get; init; }
    public long ProcessMemoryBytes { get; init; }
    public double CpuUsagePercent { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public interface IDiagnosticExportService
{
    Task<string> ExportDiagnosticsAsync(string outputPath = "");
}
