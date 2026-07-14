using AnayPublisherStudio.Domain.Plugins;
using AnayPublisherStudio.Infrastructure.Plugins;
using Xunit;

namespace AnayPublisherStudio.Tests.Plugins;

public class PluginManagerTests
{
    [Fact]
    public void Discover_FindsManifests()
    {
        var root = Path.Combine(Path.GetTempPath(), "aps-plugins-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "p1"));
        File.WriteAllText(Path.Combine(root, "p1", "plugin.json"),
            """{"id":"p1","name":"P1","version":"1.0.0","kind":"Templates"}""");

        try
        {
            var mgr = new PluginManager(root);
            var found = mgr.Discover();
            Assert.Contains(found, m => m.Id == "p1");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public async Task Load_WithoutAssembly_RegistersManifest()
    {
        var root = Path.Combine(Path.GetTempPath(), "aps-plugins-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mgr = new PluginManager(root);
            var manifest = new PluginManifest { Id = "x", Name = "X", Kind = PluginKind.Dictionaries };
            await mgr.LoadAsync(manifest);
            Assert.Contains(mgr.Loaded, m => m.Id == "x");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
