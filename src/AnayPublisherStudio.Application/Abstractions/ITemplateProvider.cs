using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>Supplies data-driven publishing templates by id.</summary>
public interface ITemplateProvider
{
    /// <summary>Lists every discoverable template.</summary>
    IReadOnlyList<PublishingTemplate> ListTemplates();

    /// <summary>Loads a single template by its id, or null if not found.</summary>
    PublishingTemplate? GetTemplate(string id);
}
