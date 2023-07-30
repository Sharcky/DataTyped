namespace DataTyped.Model;

public class CodeAnalysisResult
{
    public string? ProjectRootPath { get; set; }

    private List<TypeDefinition> _types = new();
    
    public IEnumerable<TypeDefinition> TypeDefinitions => _types;

    public void AddOrMerge(TypeDefinition typeDefinition)
    {
        var existing = _types.FirstOrDefault(x => x.FullName == typeDefinition.FullName);
        if (existing != null)
            existing.Merge(typeDefinition);
        else 
            _types.Add(typeDefinition);
    }
}
