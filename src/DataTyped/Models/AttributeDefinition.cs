namespace DataTyped.Model;

public record AttributeDefinition(string Name, List<ParameterDefinition> Parameters)
{
    public AttributeDefinition(string Name): this(Name, new List<ParameterDefinition>()) { }

    public AttributeDefinition(string Name, params object[] Parameters): this(Name, (Parameters ?? Array.Empty<object>()).Select(x => new ParameterDefinition(x)).ToList()) { }
}
