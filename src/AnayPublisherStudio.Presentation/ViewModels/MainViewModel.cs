using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Application.Configuration;
using AnayPublisherStudio.Domain.Layout;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace AnayPublisherStudio.Presentation.ViewModels;

/// <summary>
/// Primary view-model backing the shell. Holds no business logic itself; it
/// delegates to the Application engines resolved through DI, keeping the UI
/// free of publishing rules (Clean Architecture / MVVM).
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IDocumentParser _parser;
    private readonly IExportService _exporter;
    private readonly IProjectRepository _projects;
    private readonly IAiAssistant _ai;
    private readonly ISettingsService _settings;
    private readonly ITemplateProvider? _templates;
    private readonly ILivePreviewEngine? _preview;
    private readonly ICoverDesigner? _coverDesigner;
    private readonly ISpineCalculator? _spine;
    private readonly IValidationEngine? _validator;
    private readonly IProfessionalLayoutEngine? _layout;

    private PublishingProject _project = new();
    private BookDocument _book = new();
    private LayoutDocument? _layoutDoc;
    private PublishingTemplate? _activeTemplate;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _author = string.Empty;
    [ObservableProperty] private string _status = "Ready.";
    [ObservableProperty] private string _previewMessage = "Open a manuscript to see the live publishing preview.";
    [ObservableProperty] private string _previewHeader = string.Empty;
    [ObservableProperty] private string _previewFooter = string.Empty;
    [ObservableProperty] private string _previewModeLabel = "Preview: Single page";
    [ObservableProperty] private string _previewDetails = "Trim / bleed / margins / live area guides available after layout.";
    [ObservableProperty] private string _guidesLabel = string.Empty;
    [ObservableProperty] private string _zoomLabel = "Zoom 100%";
    [ObservableProperty] private string _pageCountLabel = "of 0";
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private double _previewPageWidth = 432; // 6in * 72
    [ObservableProperty] private double _previewPageHeight = 648; // 9in * 72
    [ObservableProperty] private bool _showGuides = true;
    [ObservableProperty] private string _previewMode = "single";
    [ObservableProperty] private string? _selectedTemplateId;

    /// <summary>Chapters shown in the structure tree.</summary>
    public ObservableCollection<Chapter> Chapters { get; } = new();

    /// <summary>Validation findings shown in the side panel.</summary>
    public ObservableCollection<string> Findings { get; } = new();

    /// <summary>Bookmark titles (presentation navigation).</summary>
    public ObservableCollection<string> Bookmarks { get; } = new();

    /// <summary>Asset labels.</summary>
    public ObservableCollection<string> Assets { get; } = new();

    /// <summary>Available template ids.</summary>
    public ObservableCollection<string> Templates { get; } = new();

    /// <summary>Settings ViewModel for the Settings panel.</summary>
    public SettingsViewModel Settings { get; }

    /// <summary>Creates the view-model from its injected dependencies.</summary>
    public MainViewModel(IDocumentParser parser, IExportService exporter,
        IProjectRepository projects, IAiAssistant ai, ISettingsService settings,
        ITemplateProvider? templates = null,
        ILivePreviewEngine? preview = null,
        ICoverDesigner? coverDesigner = null,
        ISpineCalculator? spine = null,
        IValidationEngine? validator = null,
        IProfessionalLayoutEngine? layout = null)
    {
        _parser = parser;
        _exporter = exporter;
        _projects = projects;
        _ai = ai;
        _settings = settings;
        _templates = templates;
        _preview = preview;
        _coverDesigner = coverDesigner;
        _spine = spine;
        _validator = validator;
        _layout = layout;

        Settings = new SettingsViewModel(settings);
        _selectedTemplateId = settings.Options.App.DefaultTemplateId;

        if (_templates is not null)
        {
            foreach (var t in _templates.ListTemplates())
                Templates.Add(t.Id);
        }
        if (Templates.Count == 0)
            Templates.Add("amazon-paperback-6x9");

        if (!string.IsNullOrEmpty(settings.UserSettings.ThemeOverride))
            ThemeManager.ApplyTheme(settings.UserSettings.ThemeOverride, settings.UserSettings.AccentColorOverride);
    }

    [RelayCommand]
    private void OpenManuscript()
    {
        var dlg = new OpenFileDialog { Filter = "Word Documents (*.docx)|*.docx" };
        if (dlg.ShowDialog() != true) return;

        _project.ManuscriptPath = dlg.FileName;
        using var fs = File.OpenRead(dlg.FileName);
        _book = _parser.Parse(fs);

        Title = _book.Metadata.Title;
        Author = _book.Metadata.Author;
        Chapters.Clear();
        foreach (var c in _book.Chapters) Chapters.Add(c);

        Bookmarks.Clear();
        foreach (var c in _book.Chapters.Where(c => c.Number > 0))
            Bookmarks.Add(c.Title);

        Assets.Clear();
        Assets.Add(Path.GetFileName(dlg.FileName));
        if (!string.IsNullOrEmpty(_project.FrontCoverImagePath))
            Assets.Add("Front cover image");

        Status = $"Opened '{Path.GetFileName(dlg.FileName)}'.";
        _ = RefreshPreviewAsync();
    }

    [RelayCommand]
    private async Task SaveProject()
    {
        _project.Metadata.Title = Title;
        _project.Metadata.Author = Author;
        if (!string.IsNullOrEmpty(SelectedTemplateId))
            _project.TemplateId = SelectedTemplateId;
        await _projects.SaveAsync(_project);
        Status = "Project saved.";
    }

    [RelayCommand]
    private async Task Publish()
    {
        if (string.IsNullOrEmpty(_project.ManuscriptPath)) { Status = "Open a manuscript first."; return; }
        _project.Metadata.Title = Title;
        _project.Metadata.Author = Author;
        if (!string.IsNullOrEmpty(SelectedTemplateId))
            _project.TemplateId = SelectedTemplateId;

        var outDir = Path.Combine(Path.GetDirectoryName(_project.ManuscriptPath)!, "output");
        Status = "Publishing...";
        var result = await _exporter.PublishAsync(_project, outDir);

        Findings.Clear();
        foreach (var f in result.Validation.Findings) Findings.Add(f.ToString());
        Status = result.Validation.IsPublishable
            ? $"Exported {result.PageCount} pages. Ready to publish."
            : $"Exported with {result.Validation.ErrorCount} blocking issue(s).";
        PageCountLabel = $"of {result.PageCount}";
    }

    [RelayCommand]
    private async Task GenerateDescription()
    {
        _book.Metadata.Description = await _ai.GenerateDescriptionAsync(_book);
        Status = "AI description suggested (user approval required to apply).";
    }

    [RelayCommand]
    private async Task SuggestKeywords()
    {
        var kw = await _ai.SuggestKeywordsAsync(_book);
        _book.Metadata.Keywords = kw.ToList();
        Status = $"AI suggested {kw.Count} keywords (user approval required).";
    }

    [RelayCommand]
    private async Task SuggestChecklist()
    {
        var items = await _ai.SuggestPublishingChecklistAsync(_book);
        Findings.Clear();
        foreach (var i in items) Findings.Add("CHECK: " + i);
        Status = "Publishing checklist generated (suggestions only).";
    }

    [RelayCommand]
    private async Task SuggestToc()
    {
        var toc = await _ai.SuggestTocAsync(_book);
        Bookmarks.Clear();
        foreach (var t in toc) Bookmarks.Add(t);
        Status = "TOC suggestions ready (user approval required).";
    }

    [RelayCommand]
    private void ToggleTheme() => ThemeManager.Toggle();

    [RelayCommand]
    private async Task RefreshPreview() => await RefreshPreviewAsync();

    [RelayCommand] private void PreviewSingle() { PreviewMode = "single"; PreviewModeLabel = "Preview: Single page"; }
    [RelayCommand] private void PreviewFacing() { PreviewMode = "facing"; PreviewModeLabel = "Preview: Facing pages"; }
    [RelayCommand] private void PreviewContinuous() { PreviewMode = "continuous"; PreviewModeLabel = "Preview: Continuous"; }
    [RelayCommand]
    private void ToggleGuides()
    {
        ShowGuides = !ShowGuides;
        UpdateGuidesLabel();
    }

    [RelayCommand]
    private void ZoomIn()
    {
        Zoom = Math.Min(3.0, Zoom + 0.1);
        ApplyZoom();
    }

    [RelayCommand]
    private void ZoomOut()
    {
        Zoom = Math.Max(0.4, Zoom - 0.1);
        ApplyZoom();
    }

    [RelayCommand]
    private void PrevPage()
    {
        if (CurrentPage > 1) { CurrentPage--; UpdatePreviewPage(); }
    }

    [RelayCommand]
    private void NextPage()
    {
        var max = _layoutDoc?.PageCount ?? 1;
        if (CurrentPage < max) { CurrentPage++; UpdatePreviewPage(); }
    }

    [RelayCommand]
    private void DesignCover()
    {
        if (_coverDesigner is null || _activeTemplate is null) { Status = "Cover designer not available."; return; }
        var pages = _layoutDoc?.PageCount ?? 24;
        var design = _coverDesigner.CreateDesign(_project, _activeTemplate, pages);
        Status = $"Cover design ready: {design.OverallWidth:0.###}×{design.OverallHeight:0.###}in, spine {design.SpineWidth:0.###}in, {design.Layers.Count} layers.";
        PreviewDetails = $"Cover layers: {string.Join(", ", design.Layers.Select(l => l.Name))}";
    }

    [RelayCommand]
    private void ValidateCover()
    {
        if (_coverDesigner is null || _activeTemplate is null) { Status = "Cover designer not available."; return; }
        var pages = _layoutDoc?.PageCount ?? 24;
        var design = _coverDesigner.CreateDesign(_project, _activeTemplate, pages);
        if (_spine is not null)
            _coverDesigner.RecalculateSpine(design, _activeTemplate, pages, _spine);
        var issues = _coverDesigner.ValidateDesign(design, _activeTemplate);
        Findings.Clear();
        if (issues.Count == 0) Findings.Add("Cover design: no issues.");
        else foreach (var i in issues) Findings.Add("COVER: " + i);
        Status = $"Cover validation: {issues.Count} issue(s).";
    }

    [RelayCommand]
    private void RunPreflight()
    {
        if (_validator is null || _activeTemplate is null) { Status = "Validator not available."; return; }
        var pages = _layoutDoc?.PageCount ?? Math.Max(1, _book.Chapters.Count);
        var report = _validator.Validate(_book, _activeTemplate, pages);
        Findings.Clear();
        foreach (var f in report.Findings) Findings.Add(f.ToString());
        Status = report.IsPublishable
            ? "Preflight passed (no errors)."
            : $"Preflight: {report.ErrorCount} error(s), {report.WarningCount} warning(s).";
    }

    private async Task RefreshPreviewAsync()
    {
        _activeTemplate = _templates?.GetTemplate(SelectedTemplateId ?? _project.TemplateId);
        if (_activeTemplate is null)
        {
            PreviewMessage = $"{Chapters.Count} chapters, {_book.TotalBlocks} blocks parsed.";
            return;
        }

        PreviewPageWidth = _activeTemplate.TrimWidth * 72 * Zoom;
        PreviewPageHeight = _activeTemplate.TrimHeight * 72 * Zoom;

        if (_preview is not null)
        {
            _layoutDoc = await _preview.RefreshAsync(_book, _activeTemplate);
        }
        else if (_layout is not null)
        {
            _layoutDoc = await _layout.ComposeAsync(_book, _activeTemplate);
        }

        CurrentPage = 1;
        PageCountLabel = $"of {_layoutDoc?.PageCount ?? 0}";
        UpdatePreviewPage();
        UpdateGuidesLabel();
        Status = "Live preview refreshed from layout engine.";
    }

    private void UpdatePreviewPage()
    {
        if (_layoutDoc is null || _layoutDoc.Pages.Count == 0)
        {
            PreviewMessage = $"{Chapters.Count} chapters, {_book.TotalBlocks} blocks.";
            return;
        }

        var page = _layoutDoc.Pages.FirstOrDefault(p => p.PageNumber == CurrentPage)
                   ?? _layoutDoc.Pages[0];
        PreviewHeader = page.RunningHeader;
        PreviewFooter = page.RunningFooter;
        if (page.IsBlank)
        {
            PreviewMessage = "(Blank page inserted for recto chapter opening)";
        }
        else
        {
            var chapter = _book.Chapters.FirstOrDefault(c => c.Number == page.ChapterNumber)
                          ?? _book.Chapters.FirstOrDefault();
            var texts = new List<string>();
            if (chapter is not null)
            {
                foreach (var order in page.BlockOrders.Take(8))
                {
                    var block = chapter.Blocks.FirstOrDefault(b => b.Order == order)
                                ?? chapter.Blocks.ElementAtOrDefault(order);
                    if (block is Domain.Blocks.ParagraphBlock p)
                        texts.Add(p.PlainText);
                    else if (block is Domain.Blocks.HeadingBlock h)
                        texts.Add(h.Text);
                }
            }
            PreviewMessage = texts.Count > 0
                ? string.Join("\n\n", texts)
                : $"Page {page.PageNumber} ({page.Side}) — {page.BlockOrders.Count} block(s).";
        }

        PreviewDetails =
            $"Page {page.PageNumber} {page.Side}\n" +
            $"Margins I/O/T/B: {page.Margins.Inside:0.###}/{page.Margins.Outside:0.###}/{page.Margins.Top:0.###}/{page.Margins.Bottom:0.###} in\n" +
            $"Live area: {page.LiveArea.WidthInches:0.###}×{page.LiveArea.HeightInches:0.###} in\n" +
            $"Trim: {_layoutDoc.TrimWidth}×{_layoutDoc.TrimHeight} in | Bleed: {_layoutDoc.BleedInches} in";
    }

    private void ApplyZoom()
    {
        ZoomLabel = $"Zoom {Zoom * 100:0}%";
        if (_activeTemplate is not null)
        {
            PreviewPageWidth = _activeTemplate.TrimWidth * 72 * Zoom;
            PreviewPageHeight = _activeTemplate.TrimHeight * 72 * Zoom;
        }
    }

    private void UpdateGuidesLabel()
    {
        GuidesLabel = ShowGuides
            ? "Guides: trim · bleed · margins · live area · running headers/footers"
            : "Guides hidden";
    }
}
