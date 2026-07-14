using System.Text.Json;
using Microsoft.Data.Sqlite;
using AnayPublisherStudio.Application.Abstractions;
using AnayPublisherStudio.Domain.Model;

namespace AnayPublisherStudio.Infrastructure.Persistence;

/// <summary>
/// SQLite-backed <see cref="IProjectRepository"/>. Project metadata is stored
/// in columns for querying; the full project graph is serialised to JSON.
/// </summary>
public sealed class SqliteProjectRepository : IProjectRepository
{
    private readonly string _connectionString;

    /// <summary>Creates the repository and ensures the schema exists.</summary>
    public SqliteProjectRepository(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ToString();
        using var cn = Open();
        using var cmd = cn.CreateCommand();
        cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Projects (
            Id TEXT PRIMARY KEY, Name TEXT NOT NULL, ModifiedUtc TEXT NOT NULL, Payload TEXT NOT NULL);";
        cmd.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var cn = new SqliteConnection(_connectionString);
        cn.Open();
        return cn;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(PublishingProject project, CancellationToken ct = default)
    {
        project.ModifiedUtc = DateTime.UtcNow;
        await using var cn = Open();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Projects (Id, Name, ModifiedUtc, Payload)
            VALUES ($id, $name, $mod, $payload)
            ON CONFLICT(Id) DO UPDATE SET Name=$name, ModifiedUtc=$mod, Payload=$payload;";
        cmd.Parameters.AddWithValue("$id", project.Id.ToString());
        cmd.Parameters.AddWithValue("$name", project.Name);
        cmd.Parameters.AddWithValue("$mod", project.ModifiedUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(project));
        await cmd.ExecuteNonQueryAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<PublishingProject?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await using var cn = Open();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Payload FROM Projects WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        var payload = (string?)await cmd.ExecuteScalarAsync(ct);
        return payload is null ? null : JsonSerializer.Deserialize<PublishingProject>(payload);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PublishingProject>> GetRecentAsync(int max = 10, CancellationToken ct = default)
    {
        var list = new List<PublishingProject>();
        await using var cn = Open();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "SELECT Payload FROM Projects ORDER BY ModifiedUtc DESC LIMIT $max;";
        cmd.Parameters.AddWithValue("$max", max);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var p = JsonSerializer.Deserialize<PublishingProject>(reader.GetString(0));
            if (p is not null) list.Add(p);
        }
        return list;
    }
}
