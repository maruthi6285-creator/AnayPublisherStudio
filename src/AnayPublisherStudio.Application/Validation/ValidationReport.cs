using AnayPublisherStudio.Domain.Enums;

namespace AnayPublisherStudio.Application.Validation;

/// <summary>Aggregated result of running the validation engine.</summary>
public sealed class ValidationReport
{
    /// <summary>All findings, in the order the checks ran.</summary>
    public List<ValidationFinding> Findings { get; } = new();

    /// <summary>True when no finding has <see cref="ValidationSeverity.Error"/>.</summary>
    public bool IsPublishable => Findings.All(f => f.Severity != ValidationSeverity.Error);

    /// <summary>Count of error-level findings.</summary>
    public int ErrorCount => Findings.Count(f => f.Severity == ValidationSeverity.Error);

    /// <summary>Count of warning-level findings.</summary>
    public int WarningCount => Findings.Count(f => f.Severity == ValidationSeverity.Warning);

    /// <summary>Adds a finding to the report.</summary>
    public void Add(string check, ValidationSeverity severity, string message)
        => Findings.Add(new ValidationFinding { Check = check, Severity = severity, Message = message });
}
