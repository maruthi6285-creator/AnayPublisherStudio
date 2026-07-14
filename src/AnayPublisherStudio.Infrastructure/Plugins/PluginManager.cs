using System.Runtime.Loader;
using System.Text.Json;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Plugins;

namespace AnayPublisherStudio.Infrastructure.Plugins;

/// <summary>
/// Discovers plugin packages (plugin.json) and optionally loads assemblies via
/// <see cref="AssemblyLoadContext"/> for isolation.
/// </summary>
public sealed class PluginManager : IPluginManager
{
    private readonly string _pluginsRoot;
    private readonly List<PluginManifest> _loaded = new();
    private readonly List<AssemblyLoadContext> _contexts = new();

    /// <summary>Creates a manager rooted at the plugins directory.</summary>
    public PluginManager(string pluginsRoot)
    {
        _pluginsRoot = pluginsRoot;
        Directory.CreateDirectory(_pluginsRoot);
    }

    /// <inheritdoc/>
    public IReadOnlyList<PluginManifest> Loaded => _loaded;

    /// <inheritdoc/>
    public IReadOnlyList<PluginManifest> Discover()
    {
        var list = new List<PluginManifest>();
        if (!Directory.Exists(_pluginsRoot)) return list;

        foreach (var file in Directory.EnumerateFiles(_pluginsRoot, "plugin.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(file);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                var manifest = JsonSerializer.Deserialize<PluginManifest>(json, options);
                if (manifest is null) continue;
                if (string.IsNullOrEmpty(manifest.Id))
                    manifest.Id = Path.GetFileName(Path.GetDirectoryName(file)) ?? manifest.Name;
                list.Add(manifest);
            }
            catch { /* skip malformed */ }
        }
        return list;
    }

    /// <inheritdoc/>
    public Task<object?> LoadAsync(PluginManifest manifest, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(manifest.Assembly))
        {
            if (!_loaded.Any(m => m.Id == manifest.Id))
                _loaded.Add(manifest);
            return Task.FromResult<object?>(manifest);
        }

        var dir = Directory.EnumerateFiles(_pluginsRoot, "plugin.json", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .FirstOrDefault(d =>
            {
                try
                {
                    var m = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(Path.Combine(d!, "plugin.json")),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return m is not null && string.Equals(m.Id, manifest.Id, StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            });

        if (dir is null)
            return Task.FromResult<object?>(null);

        var asmPath = Path.Combine(dir, manifest.Assembly);
        if (!File.Exists(asmPath))
            return Task.FromResult<object?>(null);

        var alc = new AssemblyLoadContext("plugin-" + manifest.Id, isCollectible: true);
        _contexts.Add(alc);
        var assembly = alc.LoadFromAssemblyPath(Path.GetFullPath(asmPath));
        object? instance = null;
        if (!string.IsNullOrEmpty(manifest.EntryType))
        {
            var type = assembly.GetType(manifest.EntryType);
            if (type is not null)
                instance = Activator.CreateInstance(type);
        }

        if (!_loaded.Any(m => m.Id == manifest.Id))
            _loaded.Add(manifest);

        return Task.FromResult(instance);
    }
}
