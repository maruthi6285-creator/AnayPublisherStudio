namespace AnayPublisherStudio.Domain.Plugins;

public sealed class PluginManifest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public PluginKind Kind { get; set; } = PluginKind.Templates;
    public string? Assembly { get; set; }
    public string? EntryType { get; set; }
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? Website { get; set; }
    public string? License { get; set; }
    public string? MinAppVersion { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public string? Signature { get; set; }
    public string? SignerCertificate { get; set; }
    public bool IsSystem { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string? PluginDirectory { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasAssembly => !string.IsNullOrEmpty(Assembly);
}
