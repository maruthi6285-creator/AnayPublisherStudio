namespace AnayPublisherStudio.Application.Abstractions;

public interface IAssetManager
{
    IReadOnlyList<AssetEntry> Assets { get; }
    event Action? AssetsChanged;
    void AddAsset(string filePath);
    void RemoveAsset(string id);
    void Refresh();
    void SetTag(string assetId, string tag);
    IReadOnlyList<AssetEntry> Search(string query);
    IReadOnlyList<AssetEntry> GetByTag(string tag);
    IReadOnlyList<string> Tags { get; }
}

public sealed record AssetEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string Type { get; init; } = "Unknown";
    public string Icon { get; init; } = "📄";
    public long SizeBytes { get; init; }
    public DateTime AddedUtc { get; init; } = DateTime.UtcNow;
    public List<string> Tags { get; init; } = new();
}
