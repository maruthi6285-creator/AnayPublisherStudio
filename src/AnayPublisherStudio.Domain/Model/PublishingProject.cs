using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Domain.Model;

/// <summary>
/// A user project: the manuscript plus the chosen publishing target and assets.
/// Persisted to SQLite and surfaced in "Recent Projects".
/// </summary>
public sealed class PublishingProject
{
    /// <summary>Stable project identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Display name.</summary>
    public string Name { get; set; } = "Untitled Project";

    /// <summary>Absolute path to the source DOCX manuscript.</summary>
    public string? ManuscriptPath { get; set; }

    /// <summary>Identifier of the selected template (e.g. "amazon-paperback-6x9").</summary>
    public string TemplateId { get; set; } = "amazon-paperback-6x9";

    /// <summary>Book metadata edited by the user.</summary>
    public BookMetadata Metadata { get; set; } = new();

    /// <summary>Absolute path to the front cover image, if provided.</summary>
    public string? FrontCoverImagePath { get; set; }

    /// <summary>Absolute path to the back cover image, if provided.</summary>
    public string? BackCoverImagePath { get; set; }

    /// <summary>Absolute path to the author photo, if provided.</summary>
    public string? AuthorImagePath { get; set; }

    /// <summary>Absolute path to the publisher logo, if provided.</summary>
    public string? LogoImagePath { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the most recent modification.</summary>
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;
}
