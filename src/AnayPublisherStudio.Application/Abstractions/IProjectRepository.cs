using AnayPublisherStudio.Domain.Model;

namespace AnayPublisherStudio.Application.Abstractions;

/// <summary>Persistence for user projects (SQLite-backed).</summary>
public interface IProjectRepository
{
    /// <summary>Inserts or updates a project.</summary>
    Task SaveAsync(PublishingProject project, CancellationToken ct = default);

    /// <summary>Loads a project by id, or null.</summary>
    Task<PublishingProject?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns recent projects, most-recently-modified first.</summary>
    Task<IReadOnlyList<PublishingProject>> GetRecentAsync(int max = 10, CancellationToken ct = default);
}
