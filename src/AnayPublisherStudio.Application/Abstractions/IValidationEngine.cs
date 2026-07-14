using AnayPublisherStudio.Application.Validation;
using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>Runs KDP-compliance checks over a book and its template.</summary>
public interface IValidationEngine
{
    /// <summary>Validates the book against the template and returns a report.</summary>
    ValidationReport Validate(BookDocument book, PublishingTemplate template, int pageCount);
}
