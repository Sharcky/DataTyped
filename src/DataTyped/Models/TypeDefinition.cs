using DataTyped.Generator;
using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace DataTyped.Model;

public enum AccessModifier
{
    Private = 1,
    Protected = 2,
    Public = 3,
    Internal = 4,
    PrivateProtected = 5,
    ProtectedInternal = 6
}

[DebuggerDisplay("{FullName}")]
/// <summary>
/// Represents a type definition
/// </summary>
public class TypeDefinition
{
    /// <summary>
    /// Create a new instance of this class based on its name.
    /// </summary>
    /// <param name="name"></param>
    public TypeDefinition(string name)
    {
        Name = name;
    }

    public AccessModifier? AccessModifier { get; set; }

    public bool IsRecord { get; set; }

    public string? Namespace { get; set; }

    /// <summary>
    /// The name of the class, that should be valid in C#.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The properties that can be generated inside the class.
    /// </summary>
    public List<PropertyDefinition> Properties { get; } = new();

    /// <summary>
    /// The name of the type in the data source.
    /// </summary>
    public string? OriginalName { get; set; }

    public DataSource? DataSource { get; set; }

    public List<AttributeDefinition> Attributes { get; } = new();

    public bool IsExplicit { get; set; }

    public string FullName
    {
        get
        {
            var result = "";
            if (!Namespace.IsNullOrEmpty())
            {
                result += Namespace;
                result += ".";
            }
            result += Name;
            return result;
        }
    }
}
