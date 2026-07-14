using System.Text.Json;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Infrastructure.Templates;

/// <summary>
/// Discovers publishing templates by scanning a root folder for
/// <c>template.json</c> files. Keeps the engines free of hard-coded KDP values.
/// Also merges margins.json / layout.json / publisher.json when present (Template SDK).
/// </summary>
public sealed class JsonTemplateProvider : ITemplateProvider
{
    private readonly string _root;
    private readonly Dictionary<string, PublishingTemplate> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a provider rooted at the given templates directory.</summary>
    public JsonTemplateProvider(string templatesRoot)
    {
        _root = templatesRoot;
        Reload();
    }

    /// <summary>Re-scans the templates directory.</summary>
    public void Reload()
    {
        _cache.Clear();
        if (!Directory.Exists(_root)) return;
        foreach (var file in Directory.EnumerateFiles(_root, "template.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(file);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                var tpl = JsonSerializer.Deserialize<PublishingTemplate>(json, options);
                if (tpl is null) continue;
                if (string.IsNullOrEmpty(tpl.Id))
                    tpl.Id = Path.GetFileName(Path.GetDirectoryName(file)) ?? tpl.Name;

                var dir = Path.GetDirectoryName(file)!;
                tpl.PackagePath = dir;
                MergeMargins(tpl, Path.Combine(dir, "margins.json"));
                MergeLayout(tpl, Path.Combine(dir, "layout.json"));
                MergePublisher(tpl, Path.Combine(dir, "publisher.json"));

                _cache[tpl.Id] = tpl;
            }
            catch { /* skip malformed template */ }
        }
    }

    private static void MergeMargins(PublishingTemplate tpl, string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.TryGetProperty("insideMargin", out var im)) tpl.InsideMargin = im.GetDouble();
            if (root.TryGetProperty("outsideMargin", out var om)) tpl.OutsideMargin = om.GetDouble();
            if (root.TryGetProperty("topMargin", out var tm)) tpl.TopMargin = tm.GetDouble();
            if (root.TryGetProperty("bottomMargin", out var bm)) tpl.BottomMargin = bm.GetDouble();
            if (root.TryGetProperty("mirrorMargins", out var mm)) tpl.MirrorMargins = mm.GetBoolean();
            if (root.TryGetProperty("gutterMinByPageCount", out var gutters) && gutters.ValueKind == JsonValueKind.Array)
            {
                tpl.GutterByPageCount = new List<GutterRule>();
                foreach (var g in gutters.EnumerateArray())
                {
                    tpl.GutterByPageCount.Add(new GutterRule
                    {
                        MaxPages = g.TryGetProperty("maxPages", out var mp) ? mp.GetInt32() : 0,
                        Inside = g.TryGetProperty("inside", out var inn) ? inn.GetDouble() : tpl.InsideMargin,
                    });
                }
            }
        }
        catch { /* ignore */ }
    }

    private static void MergeLayout(PublishingTemplate tpl, string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            var rules = JsonSerializer.Deserialize<CompositionRules>(File.ReadAllText(path), options);
            if (rules is not null)
                tpl.Composition = rules;
        }
        catch { /* ignore */ }
    }

    private static void MergePublisher(PublishingTemplate tpl, string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            if (root.TryGetProperty("platform", out var p)) tpl.Platform = p.GetString() ?? tpl.Platform;
            if (root.TryGetProperty("name", out var n) && string.IsNullOrEmpty(tpl.Name))
                tpl.Name = n.GetString() ?? tpl.Name;
        }
        catch { /* ignore */ }
    }

    /// <inheritdoc/>
    public IReadOnlyList<PublishingTemplate> ListTemplates() => _cache.Values.ToList();

    /// <inheritdoc/>
    public PublishingTemplate? GetTemplate(string id)
        => _cache.TryGetValue(id, out var t) ? t : null;
}
