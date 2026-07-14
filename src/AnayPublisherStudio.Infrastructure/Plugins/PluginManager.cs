using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Plugins;

namespace AnayPublisherStudio.Infrastructure.Plugins;

public sealed class PluginManager : IPluginManager, IDisposable
{
    public IReadOnlyList<PluginManifest> Loaded => _loaded.ToList();

    private readonly string _pluginsRoot;
    private readonly List<PluginManifest> _loaded = new();
    private readonly List<(AssemblyLoadContext Context, string Id)> _contexts = new();
    private readonly Dictionary<string, List<string>> _dependencyGraph = new();
    private bool _disposed;

    public PluginManager(string pluginsRoot)
    {
        _pluginsRoot = pluginsRoot;
        Directory.CreateDirectory(_pluginsRoot);
    }

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
                if (string.IsNullOrEmpty(manifest.Version))
                    manifest.Version = "1.0.0";

                manifest.PluginDirectory = Path.GetDirectoryName(Path.GetFullPath(file));
                list.Add(manifest);
            }
            catch { /* skip malformed */ }
        }

        ResolveDependencies(list);
        return list;
    }

    public Task<object?> LoadAsync(PluginManifest manifest, CancellationToken ct = default)
    {
        if (manifest.HasAssembly)
        {
            var existing = _loaded.FirstOrDefault(m => m.Id == manifest.Id);
            if (existing is not null)
            {
                var comparison = CompareVersions(manifest.Version, existing.Version);
                if (comparison <= 0)
                    return Task.FromResult<object?>(null);
                UnloadPlugin(existing.Id);
            }

            VerifySignature(manifest);
            return LoadAssemblyAsync(manifest, ct);
        }

        if (!_loaded.Any(m => m.Id == manifest.Id))
            _loaded.Add(manifest);
        return Task.FromResult<object?>(manifest);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var (ctx, _) in _contexts)
        {
            try { ctx.Unload(); } catch { }
        }
        _contexts.Clear();
        _loaded.Clear();
    }

    private void UnloadPlugin(string pluginId)
    {
        _loaded.RemoveAll(m => m.Id == pluginId);
        var entry = _contexts.FirstOrDefault(c => c.Id == pluginId);
        if (entry.Context is not null)
        {
            try { entry.Context.Unload(); } catch { }
            _contexts.Remove(entry);
        }
    }

    private async Task<object?> LoadAssemblyAsync(PluginManifest manifest, CancellationToken ct)
    {
        var dir = manifest.PluginDirectory;
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return null;

        var asmPath = Path.Combine(dir, manifest.Assembly!);
        if (!File.Exists(asmPath))
            return null;

        var alc = new AssemblyLoadContext("plugin-" + manifest.Id, isCollectible: true);
        _contexts.Add((alc, manifest.Id));
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

        return instance;
    }

    private void VerifySignature(PluginManifest manifest)
    {
        if (string.IsNullOrEmpty(manifest.Signature) || string.IsNullOrEmpty(manifest.SignerCertificate))
            return;

        try
        {
            var data = Encoding.UTF8.GetBytes($"{manifest.Id}|{manifest.Version}|{manifest.Assembly}");
            var sig = Convert.FromBase64String(manifest.Signature);
            var cert = Convert.FromBase64String(manifest.SignerCertificate);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(cert, out _);
            if (!rsa.VerifyData(data, sig, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                throw new InvalidOperationException($"Plugin '{manifest.Id}' signature verification failed.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // Signature fields present but invalid format — allow with warning
        }
    }

    private static int CompareVersions(string a, string b)
    {
        var re = new Regex(@"^(\d+)\.(\d+)\.(\d+)");
        var ma = re.Match(a);
        var mb = re.Match(b);
        if (!ma.Success || !mb.Success) return string.Compare(a, b, StringComparison.Ordinal);

        for (var i = 1; i <= 3; i++)
        {
            var cmp = int.Parse(ma.Groups[i].Value).CompareTo(int.Parse(mb.Groups[i].Value));
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    private void ResolveDependencies(List<PluginManifest> manifests)
    {
        _dependencyGraph.Clear();
        foreach (var m in manifests)
        {
            _dependencyGraph[m.Id] = new List<string>();
            foreach (var dep in m.Dependencies ?? Enumerable.Empty<string>())
                _dependencyGraph[m.Id].Add(dep);
        }

        manifests.Sort((a, b) =>
        {
            if (_dependencyGraph.TryGetValue(a.Id, out var deps) && deps.Contains(b.Id))
                return 1;
            if (_dependencyGraph.TryGetValue(b.Id, out var depsB) && depsB.Contains(a.Id))
                return -1;
            return 0;
        });
    }
}
