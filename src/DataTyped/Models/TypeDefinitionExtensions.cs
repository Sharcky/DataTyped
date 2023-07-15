namespace DataTyped.Model;

public static class TypeDefinitionExtensions
{
    private static readonly string[] numericPropertyTypeOrder = new[]
    {
        "int",
        "long",
        "double",
        "decimal"
    };

    /// <summary>
    /// Takes properties from a similar ClassModel and adds them here, to get a more complete version of the ClassModel.
    /// </summary>
    /// <param name="other">The ClassModel to get additional properties from</param>
    public static void Merge(this TypeDefinition typeDefinition, TypeDefinition other)
    {
        if (other != null)
        {
            var existingProps =
                typeDefinition.Properties
                              .ToDictionary(x => x.Name, x => x);

            typeDefinition.AccessModifier ??= other.AccessModifier;

            foreach (var otherProp in other.Properties)
            {
                var existingProp =
                    existingProps.TryGetValue(otherProp.Name, out var existing)
                    ? existing
                    : null;

                if (existingProp == null)
                {
                    typeDefinition.Properties.Add(otherProp with { });
                }
                else if (existingProp.Type != otherProp.Type)
                {
                    // If there is a less restrictive property type that is needed, it must be changed
                    if (numericPropertyTypeOrder.Contains(existingProp.Type)
                        && numericPropertyTypeOrder.Contains(otherProp.Type)
                        && Array.IndexOf(numericPropertyTypeOrder, existingProp.Type) < Array.IndexOf(numericPropertyTypeOrder, otherProp.Type))
                    {
                        existingProp = existingProp with { Type = otherProp.Type };
                    }
                    else if (existingProp.Type == "DateTime" && otherProp.Type == "string")
                    {
                        existingProp = existingProp with { Type = otherProp.Type };
                    }
                }
            }
        }
    }
}