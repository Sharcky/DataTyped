namespace DataTyped.Model;

public record AttributeDefinition
{
    public string Name { get; init; }
    public string? SymbolName { get; init; }
    
    public IEnumerable<ParameterDefinition> Parameters { get; init; }
}