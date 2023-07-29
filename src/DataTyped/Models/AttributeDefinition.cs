using Microsoft.CodeAnalysis;

namespace DataTyped.Model;



internal abstract class SyntaxProvider<T> where T: SyntaxNode
{

}

public record AttributeDefinition
{
    public string Name { get; init; }
    public string? SymbolName { get; init; }
    
    public IEnumerable<ParameterDefinition> Parameters { get; init; }
}