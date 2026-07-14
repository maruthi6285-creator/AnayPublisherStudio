using System.IO.Compression;
using System.Text.Json;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Infrastructure.Templates;

/// <summary>
/// Publishing Template SDK. Each package contains template.json, styles.json,
/// fonts.json, layout.json, cover.json, preview.png, publisher.json.
/// Packages are installable into the templates root.
/// </summary>
public sealed class TemplatePackageService : ITemplatePackageService
{
    private readonly string _templatesRoot;
    private readonly ITemplateProvider _provider;

    /// <summary>Creates the service rooted at the templates directory.</summary>
    public TemplatePackageService(string templatesRoot, ITemplateProvider provider)
    {
        _templatesRoot = templatesRoot;
        _provider = provider;
        Directory.CreateDirectory(_templatesRoot);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> ListInstalledPackages()
        => Directory.EnumerateFiles(_templatesRoot, "template.json", SearchOption.AllDirectories)
            .Select(f =>
            {
                try
                {
                    var json = File.ReadAllText(f);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("id", out var id))
                        return id.GetString() ?? Path.GetFileName(Path.GetDirectoryName(f))!;
                }
                catch { /* skip */ }
                return Path.GetFileName(Path.GetDirectoryName(f)) ?? f;
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

    /// <inheritdoc/>
    public async Task InstallAsync(string packagePath, CancellationToken ct = default)
    {
        if (Directory.Exists(packagePath))
        {
            var id = ReadId(Path.Combine(packagePath, "template.json")) ?? Path.GetFileName(packagePath);
            var dest = Path.Combine(_templatesRoot, "Installed", id);
            CopyDirectory(packagePath, dest);
        }
        else if (File.Exists(packagePath) && packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var temp = Path.Combine(Path.GetTempPath(), "aps-tpl-" + Guid.NewGuid().ToString("N"));
            ZipFile.ExtractToDirectory(packagePath, temp);
            var id = FindTemplateId(temp) ?? Path.GetFileNameWithoutExtension(packagePath);
            var dest = Path.Combine(_templatesRoot, "Installed", id);
            if (Directory.Exists(dest)) Directory.Delete(dest, true);
            Directory.Move(temp, dest);
        }
        else
        {
            throw new FileNotFoundException("Template package not found.", packagePath);
        }

        // Refresh provider cache when possible.
        if (_provider is JsonTemplateProvider jtp)
            jtp.Reload();

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UninstallAsync(string packageId, CancellationToken ct = default)
    {
        var installed = Path.Combine(_templatesRoot, "Installed", packageId);
        if (Directory.Exists(installed))
            Directory.Delete(installed, true);
        if (_provider is JsonTemplateProvider jtp)
            jtp.Reload();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public TemplatePackage? GetPackage(string templateId)
    {
        var template = _provider.GetTemplate(templateId);
        if (template is null) return null;

        var root = template.PackagePath
                   ?? FindPackageRoot(templateId)
                   ?? _templatesRoot;

        return new TemplatePackage
        {
            Id = templateId,
            RootPath = root,
            Template = template,
            PreviewImagePath = FirstExisting(root, "preview.png", "cover.png"),
            PublisherJson = ReadOptional(root, "publisher.json"),
            LayoutJson = ReadOptional(root, "layout.json"),
            CoverJson = ReadOptional(root, "cover.json", "coverareas.json"),
            StylesJson = ReadOptional(root, "styles.json"),
            FontsJson = ReadOptional(root, "fonts.json"),
        };
    }

    private string? FindPackageRoot(string templateId)
    {
        foreach (var file in Directory.EnumerateFiles(_templatesRoot, "template.json", SearchOption.AllDirectories))
        {
            var id = ReadId(file);
            if (string.Equals(id, templateId, StringComparison.OrdinalIgnoreCase))
                return Path.GetDirectoryName(file);
        }
        return null;
    }

    private static string? FindTemplateId(string root)
    {
        var file = Directory.EnumerateFiles(root, "template.json", SearchOption.AllDirectories).FirstOrDefault();
        return file is null ? null : ReadId(file);
    }

    private static string? ReadId(string templateJsonPath)
    {
        if (!File.Exists(templateJsonPath)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(templateJsonPath));
            if (doc.RootElement.TryGetProperty("id", out var id))
                return id.GetString();
        }
        catch { /* ignore */ }
        return null;
    }

    private static string? ReadOptional(string root, params string[] names)
    {
        foreach (var n in names)
        {
            var p = Path.Combine(root, n);
            if (File.Exists(p)) return File.ReadAllText(p);
        }
        // search one level deep
        if (Directory.Exists(root))
        {
            foreach (var n in names)
            {
                var hit = Directory.EnumerateFiles(root, n, SearchOption.AllDirectories).FirstOrDefault();
                if (hit is not null) return File.ReadAllText(hit);
            }
        }
        return null;
    }

    private static string? FirstExisting(string root, params string[] names)
    {
        foreach (var n in names)
        {
            var p = Path.Combine(root, n);
            if (File.Exists(p)) return p;
        }
        if (Directory.Exists(root))
        {
            foreach (var n in names)
            {
                var hit = Directory.EnumerateFiles(root, n, SearchOption.AllDirectories).FirstOrDefault();
                if (hit is not null) return hit;
            }
        }
        return null;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
