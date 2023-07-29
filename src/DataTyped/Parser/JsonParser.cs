using Microsoft.CodeAnalysis;
using System.Text.Json;
using DataTyped.Model;

namespace DataTyped.Parser;

public class JsonParser: Parser
{
    public override List<TypeDefinition> Parse(TextReader reader, SourceProductionContext context, TypeDefinition rootType, List<TypeDefinition> types)
    {
        var input = reader.ReadToEnd();

        var json = JsonDocument.Parse(input);

        // Read the json and build a list of models that can be used to generate classes
        ResolveTypeRecursive(context, types, json.RootElement, rootType.Name, rootType);
        return types.ToList();
    }

    /// <summary>
    /// Reads json and fills the classModels list with relevant type definitions.
    /// </summary>
    /// <param name="context">The source generator context</param>
    /// <param name="types">A list that needs to be populated with resolved types</param>
    /// <param name="jsonElement">The current json element that is being read</param>
    /// <param name="typeName">The current type name that is being read</param>
    private void ResolveTypeRecursive(SourceProductionContext context, List<TypeDefinition> types, JsonElement jsonElement, string typeName, TypeDefinition rootType, string? originalName = null)
    {
        var classModel = new TypeDefinition(typeName)
        {
            AccessModifier = rootType.AccessModifier,
            Namespace = rootType.Namespace,
            IsRecord = rootType.IsRecord,
            OriginalName = originalName
        };

        // Arrays should be enumerated and handled individually
        if (jsonElement.ValueKind == JsonValueKind.Array)
        {
            var jsonArrayEnumerator = jsonElement.EnumerateArray();
            while (jsonArrayEnumerator.MoveNext())
            {
                ResolveTypeRecursive(context, types, jsonArrayEnumerator.Current, typeName, rootType);
            }

            return;
        }

        if (jsonElement.ValueKind == JsonValueKind.Object)
        {
            int orderCounter = 0;

            // Iterate the properties of the json element, they will become model properties
            foreach (JsonProperty prop in jsonElement.EnumerateObject())
            {
                string propName = RenameIfDuplicateOrConflicting(GetValidName(prop.Name), classModel);

                if (propName.Length > 0)
                {
                    PropertyDefinition property;

                    // The json value kind of the property determines how to map it to a C# type
                    switch (prop.Value.ValueKind)
                    {
                        case JsonValueKind.Array:
                            {
                                string arrPropName = GetValidName(prop.Name, true);

                                // Look at the first element in the array to determine the type of the array
                                var arrEnumerator = prop.Value.EnumerateArray();
                                if (arrEnumerator.MoveNext())
                                {
                                    if (arrEnumerator.Current.ValueKind == JsonValueKind.Number)
                                    {
                                        arrPropName = FindBestNumericType(arrEnumerator.Current);
                                    }
                                    else if (arrEnumerator.Current.ValueKind == JsonValueKind.String)
                                    {
                                        arrPropName = FindBestStringType(arrEnumerator.Current);
                                    }
                                    else if (arrEnumerator.Current.ValueKind == JsonValueKind.True || arrEnumerator.Current.ValueKind == JsonValueKind.False)
                                    {
                                        arrPropName = "bool";
                                    }
                                    else
                                    {
                                        var jsonName = pluralizer.Singularize(prop.Name);
                                        ResolveTypeRecursive(context, types, prop.Value, arrPropName, rootType, jsonName);
                                    }

                                    property = new PropertyDefinition(propName, $"IList<{arrPropName}>", prop.Name, orderCounter);
                                }
                                else
                                {
                                    property = new PropertyDefinition(propName, $"IList<object>", prop.Name, orderCounter++);
                                }

                                break;
                            }
                        case JsonValueKind.String: property = new PropertyDefinition(propName, FindBestStringType(prop.Value), prop.Name, orderCounter++); break;
                        case JsonValueKind.Number: property = new PropertyDefinition(propName, FindBestNumericType(prop.Value), prop.Name, orderCounter++); break;
                        case JsonValueKind.False:
                        case JsonValueKind.True: property = new PropertyDefinition(propName, "bool", prop.Name, orderCounter++); break;
                        case JsonValueKind.Object:
                            {
                                string objectPropName = GetValidName(prop.Name, true);

                                // Create a separate type for objects
                                ResolveTypeRecursive(context, types, prop.Value, objectPropName, rootType, prop.Name);

                                property = new PropertyDefinition(propName, objectPropName, prop.Name, orderCounter++);
                                break;
                            }
                        case JsonValueKind.Undefined:
                        case JsonValueKind.Null:
                        default: property = new PropertyDefinition(propName, "object", prop.Name, orderCounter++); break;
                    }

                    property.Attributes.Add(new AttributeDefinition
                    {
                        Name = "System.Text.Json.Serialization.JsonPropertyName",
                        SymbolName = "System.Text.Json.Serialization.JsonPropertyName",
                        Parameters = new List<ParameterDefinition>
                        {
                            new ParameterDefinition(propName)
                        }
                    });
                    classModel.Properties.Add(property);
                }
            }
        }

        // If there is already a model defined that matches by name, then we add any new properties by merging the models
        var matchingType = types.FirstOrDefault(c => string.Equals(c.Name, classModel.Name, StringComparison.InvariantCulture));
        if (matchingType != null)
        {
            matchingType.Merge(classModel);
        }
        else
        {
            // No need to merge, just add the new class model
            types.Add(classModel);
        }
    }

    /// <summary>
    /// Based on the value specified, determine an appropriate numeric type.
    /// </summary>
    /// <param name="propertyValue">Example value of the property</param>
    /// <returns>The name of the numeric type</returns>
    private string FindBestNumericType(JsonElement propertyValue)
    {
        if (propertyValue.TryGetInt32(out _))
        {
            return "int";
        }

        if (propertyValue.TryGetInt64(out _))
        {
            return "long";
        }

        if (propertyValue.TryGetDouble(out var doubleVal)
            && propertyValue.TryGetDecimal(out var decimalVal)
            && Convert.ToDecimal(doubleVal) == decimalVal)
        {
            return "double";
        }

        if (propertyValue.TryGetDecimal(out _))
        {
            return "decimal";
        }

        return "object";
    }

    /// <summary>
    /// Based on the value specified, determine if anything better than "string" can be used.
    /// </summary>
    /// <param name="current">Example value of the property</param>
    /// <returns>string or something better</returns>
    private string FindBestStringType(JsonElement propertyValue)
    {
        if (propertyValue.TryGetDateTime(out _))
        {
            return "DateTime";
        }

        return "string";
    }
}