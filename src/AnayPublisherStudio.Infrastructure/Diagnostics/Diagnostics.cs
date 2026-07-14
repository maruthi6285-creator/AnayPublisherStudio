using System.Collections.Concurrent;
using System.Diagnostics;
using AnayPublisherStudio.Application.Abstractions;

namespace AnayPublisherStudio.Infrastructure.Diagnostics;

public sealed class PerformanceMonitor : IPerformanceMonitor, IDisposable
{
    public event Action<string, double>? MetricRecorded;
    public IReadOnlyDictionary<string, double> Snapshot => new Dictionary<string, double>(_metrics);

    private readonly ConcurrentDictionary<string, double> _metrics = new();
    private readonly ConcurrentDictionary<string, Stopwatch> _activeOperations = new();

    public IDisposable BeginOperation(string name)
    {
        var sw = Stopwatch.StartNew();
        _activeOperations[name] = sw;
        return new OperationDisposable(() =>
        {
            sw.Stop();
            _activeOperations.TryRemove(name, out _);
            RecordMetric(name, sw.Elapsed.TotalMilliseconds);
        });
    }

    public void RecordMetric(string name, double milliseconds)
    {
        _metrics.AddOrUpdate(name, milliseconds, (_, existing) => (existing + milliseconds) / 2);
        MetricRecorded?.Invoke(name, milliseconds);
    }

    public void Reset()
    {
        _metrics.Clear();
        _activeOperations.Clear();
    }

    public void Dispose()
    {
        foreach (var sw in _activeOperations.Values)
            sw.Stop();
        _activeOperations.Clear();
    }

    private sealed class OperationDisposable : IDisposable
    {
        private readonly Action _onDispose;
        public OperationDisposable(Action onDispose) => _onDispose = onDispose;
        public void Dispose() => _onDispose();
    }
}

public sealed class MemoryMonitor : IMemoryMonitor, IDisposable
{
    public event Action<MemoryMetrics>? MetricsUpdated;
    public MemoryMetrics Current { get; private set; } = new();
    private Timer? _timer;
    private DateTime _lastCpuTime = DateTime.UtcNow;
    private TimeSpan _lastCpuTotal;

    public MemoryMonitor()
    {
        var process = Process.GetCurrentProcess();
        _lastCpuTotal = process.TotalProcessorTime;
    }

    public void Start(TimeSpan interval)
    {
        Stop();
        _timer = new Timer(_ => Update(), null, TimeSpan.Zero, interval);
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void Update()
    {
        var process = Process.GetCurrentProcess();
        process.Refresh();
            var now = DateTime.UtcNow;
            var cpuDelta = process.TotalProcessorTime - _lastCpuTotal;
            var timeDelta = (now - _lastCpuTime).TotalSeconds;
            var cpuPercent = timeDelta > 0 ? (cpuDelta.TotalSeconds / (timeDelta * Environment.ProcessorCount)) * 100 : 0;
            _lastCpuTime = now;
            _lastCpuTotal = process.TotalProcessorTime;

            Current = new MemoryMetrics
            {
                ManagedMemoryBytes = GC.GetTotalMemory(false),
                ProcessMemoryBytes = process.WorkingSet64,
                CpuUsagePercent = Math.Round(cpuPercent, 1),
                Timestamp = now,
            };
        MetricsUpdated?.Invoke(Current);
    }

    public void Dispose()
    {
        Stop();
    }
}

public sealed class DeveloperConsoleService : IDeveloperConsoleService
{
    public event Action<string>? MessageLogged;
    public IReadOnlyList<string> Messages => _messages.ToList();
    private readonly List<string> _messages = new();
    private readonly object _lock = new();

    public void Log(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        lock (_lock)
        {
            _messages.Add(entry);
            if (_messages.Count > 1000) _messages.RemoveRange(0, _messages.Count - 1000);
        }
        MessageLogged?.Invoke(entry);
    }

    public void LogInfo(string message) => Log($"INFO: {message}");
    public void LogWarning(string message) => Log($"WARN: {message}");
    public void LogError(string message) => Log($"ERROR: {message}");
    public void Clear() { lock (_lock) _messages.Clear(); }
}

public sealed class DiagnosticExportService : IDiagnosticExportService
{
    private readonly IPerformanceMonitor _perf;
    private readonly IMemoryMonitor _memory;
    private readonly IDeveloperConsoleService _console;

    public DiagnosticExportService(
        IPerformanceMonitor perf,
        IMemoryMonitor memory,
        IDeveloperConsoleService console)
    {
        _perf = perf;
        _memory = memory;
        _console = console;
    }

    public async Task<string> ExportDiagnosticsAsync(string outputPath = "")
    {
        if (string.IsNullOrEmpty(outputPath))
            outputPath = Path.Combine(Path.GetTempPath(), $"aps-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

        var lines = new List<string>
        {
            "===== Anay Publisher Studio Diagnostics =====",
            $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"OS: {Environment.OSVersion}",
            $".NET: {Environment.Version}",
            $"Process: {Environment.ProcessPath}",
            $"Working Set: {_memory.Current.ProcessMemoryBytes / 1024 / 1024} MB",
            $"Managed Memory: {_memory.Current.ManagedMemoryBytes / 1024 / 1024} MB",
            $"CPU: {_memory.Current.CpuUsagePercent:F1}%",
            "",
            "--- Performance Metrics ---",
        };

        foreach (var kv in _perf.Snapshot)
            lines.Add($"  {kv.Key}: {kv.Value:F2} ms");

        lines.Add("");
        lines.Add("--- Console Log ---");
        foreach (var msg in _console.Messages.TakeLast(200))
            lines.Add($"  {msg}");

        lines.Add("");
        lines.Add("===== End of Diagnostics =====");

        await File.WriteAllLinesAsync(outputPath, lines);
        return outputPath;
    }
}
