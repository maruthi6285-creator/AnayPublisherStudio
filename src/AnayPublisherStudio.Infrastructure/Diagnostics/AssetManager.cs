using System.Collections.ObjectModel;
using AnayPublisherStudio.Application.Abstractions;

namespace AnayPublisherStudio.Infrastructure.Diagnostics;

public sealed class AssetManager : IAssetManager
{
    public IReadOnlyList<AssetEntry> Assets => _assets.ToList();
    public event Action? AssetsChanged;
    public IReadOnlyList<string> Tags => _tags.Keys.ToList();

    private readonly List<AssetEntry> _assets = new();
    private readonly Dictionary<string, HashSet<string>> _tags = new();
    private readonly object _lock = new();

    public void AddAsset(string filePath)
    {
        if (!File.Exists(filePath)) return;
        var fi = new FileInfo(filePath);
        var type = fi.Extension.ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" => "Image",
            ".ttf" or ".otf" or ".woff" or ".woff2" => "Font",
            ".docx" => "Document",
            ".pdf" => "PDF",
            ".json" or ".xml" => "Data",
            _ => "Other",
        };
        var icon = type switch
        {
            "Image" => "🖼️",
            "Font" => "🔤",
            "Document" => "📄",
            "PDF" => "📕",
            "Data" => "📋",
            _ => "📁",
        };

        var entry = new AssetEntry
        {
            Name = fi.Name,
            FilePath = fi.FullName,
            Type = type,
            Icon = icon,
            SizeBytes = fi.Length,
        };

        lock (_lock)
        {
            _assets.Add(entry);
            foreach (var tag in entry.Tags)
            {
                if (!_tags.ContainsKey(tag)) _tags[tag] = new HashSet<string>();
                _tags[tag].Add(entry.Id);
            }
        }
        AssetsChanged?.Invoke();
    }

    public void RemoveAsset(string id)
    {
        lock (_lock)
        {
            var asset = _assets.FirstOrDefault(a => a.Id == id);
            if (asset is null) return;
            _assets.Remove(asset);
            foreach (var tag in asset.Tags)
            {
                if (_tags.TryGetValue(tag, out var set))
                {
                    set.Remove(id);
                    if (set.Count == 0) _tags.Remove(tag);
                }
            }
        }
        AssetsChanged?.Invoke();
    }

    public void Refresh()
    {
        lock (_lock)
        {
            var existing = _assets.ToList();
            _assets.Clear();
            foreach (var asset in existing)
            {
                if (File.Exists(asset.FilePath))
                    _assets.Add(asset);
            }
        }
        AssetsChanged?.Invoke();
    }

    public void SetTag(string assetId, string tag)
    {
        lock (_lock)
        {
            var asset = _assets.FirstOrDefault(a => a.Id == assetId);
            if (asset is null) return;
            if (!asset.Tags.Contains(tag)) asset.Tags.Add(tag);
            if (!_tags.ContainsKey(tag)) _tags[tag] = new HashSet<string>();
            _tags[tag].Add(assetId);
        }
        AssetsChanged?.Invoke();
    }

    public IReadOnlyList<AssetEntry> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Assets;
        var q = query.ToLowerInvariant();
        lock (_lock)
        {
            return _assets.Where(a =>
                a.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.Type.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                a.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
    }

    public IReadOnlyList<AssetEntry> GetByTag(string tag)
    {
        lock (_lock)
        {
            if (!_tags.TryGetValue(tag, out var ids)) return Array.Empty<AssetEntry>();
            return _assets.Where(a => ids.Contains(a.Id)).ToList();
        }
    }
}
