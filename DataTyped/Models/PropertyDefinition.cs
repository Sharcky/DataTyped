namespace DataTyped.Model;

/// <summary>
/// Represents a Property
/// </summary>
/// <param name="Name">The property Name</param>
/// <param name="Type">The property Type</param>
public record PropertyDefinition(string Name, string Type, string? OriginalName = null, int? Order = null)
{
    /// <summary>
    /// The order for JSON and CSV deserialization.
    /// </summary>
    public int? Order { get; init; } = Order;

    /// <summary>
    /// The Key property to generate strongly typed data records
    /// </summary>
    public string? Key { get; init; }

    /// <summary>
    /// the Group Key property to generate strongly typed data groups
    /// </summary>
    public string? GroupKey { get; init; }

    public string? OriginalName { get; init; } = OriginalName;

    public List<AttributeDefinition> Attributes { get; } = new();

    public bool IsExplicit { get; set; }
}