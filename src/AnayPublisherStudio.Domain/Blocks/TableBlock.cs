using AnayPublisherStudio.Domain.Enums;

namespace AnayPublisherStudio.Domain.Blocks;

/// <summary>A simple grid table (rows of string cells).</summary>
public sealed class TableBlock : ContentBlock
{
    /// <inheritdoc/>
    public override BlockType Type => BlockType.Table;

    /// <summary>Row-major cell text. First row is treated as a header.</summary>
    public List<List<string>> Rows { get; set; } = new();

    /// <summary>Column count derived from the widest row.</summary>
    public int ColumnCount => Rows.Count == 0 ? 0 : Rows.Max(r => r.Count);
}
