using AnayPublisherStudio.Domain.Enums;

namespace AnayPublisherStudio.Application.Validation;

/// <summary>A single validation result item.</summary>
public sealed class ValidationFinding
{
    /// <summary>The check that produced this finding (e.g. "ImageDpi").</summary>
    public string Check { get; init; } = string.Empty;

    /// <summary>Severity of the finding.</summary>
    public ValidationSeverity Severity { get; init; }

    /// <summary>Human-readable message.</summary>
    public string Message { get; init; } = string.Empty;

    public override string ToString() => $"[{Severity}] {Check}: {Message}";
}
