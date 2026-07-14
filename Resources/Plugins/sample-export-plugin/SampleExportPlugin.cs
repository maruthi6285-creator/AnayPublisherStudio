using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.SamplePlugin;

/// <summary>
/// Sample plugin demonstrating the plugin SDK contract.
/// Implements IPluginContract and adds a custom watermark overlay to exports.
/// </summary>
public class SampleExportPlugin : IPluginContract
{
    public string Id => "sample-export-plugin";
    public string Name => "Sample Export Plugin";
    public string Version => "1.0.0";

    private readonly List<string> _logs = new();

    public Task<bool> InitializeAsync(CancellationToken ct = default)
    {
        Log("Initializing Sample Export Plugin...");
        return Task.FromResult(true);
    }

    public Task<bool> ShutdownAsync(CancellationToken ct = default)
    {
        Log("Shutting down Sample Export Plugin.");
        return Task.FromResult(true);
    }

    /// <summary>
    /// Called before export to allow plugin to inspect/modify the pipeline.
    /// Returns the number of watermarks applied (0 = none).
    /// </summary>
    public int ApplyCustomExport(BookDocument book, PublishingTemplate template)
    {
        if (book.Metadata.Title.Contains("sample", StringComparison.OrdinalIgnoreCase))
        {
            Log($"Applied watermark for: {book.Metadata.Title}");
            return 1;
        }
        return 0;
    }

    public IReadOnlyList<string> GetLogs() => _logs.ToList();

    private void Log(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _logs.Add(entry);
        Console.WriteLine(entry);
    }
}
