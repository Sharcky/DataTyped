namespace DataTyped.Model;

public record ParameterDefinition(object Value);

public record NamedParameterDefinition(string Name, object Value): ParameterDefinition(Value);


