using Microsoft.CodeAnalysis;
using DataTyped.Model;
using CsvHelper;
using System.Globalization;
using CsvHelper.Configuration.Attributes;
using DataTyped.Generator;

namespace DataTyped.Parser;

public class CsvParser : Parser
{
    private static readonly string[] NameAttributes = new[]  { "Name",  nameof(NameAttribute) };
    private static readonly string[] IndexAttributes = new[] { "Index", nameof(IndexAttribute) };

    public override List<TypeDefinition> Parse(TextReader reader, GeneratorExecutionContext context, TypeDefinition rootType, List<TypeDefinition> types)
    {
        var existingPropertiesByName =
            from p in rootType.Properties
            from a in p.Attributes
            where a.Name.IsIn(NameAttributes)
            from par in a.Parameters
            where par.Name == null
            select (p, par?.Value);

        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();

        var values = new List<Dictionary<int, string?>>();
        while (csv.Read())
        {
            var d = new Dictionary<int, string>();

            for (int i = 0; i < csv.HeaderRecord.Length; i++)
            {
                d[i] = csv.GetField(i);
            }

            values.Add(d);
            if (values.Count >= 10)
                break;
        }

        var dataTypes =
            csv.HeaderRecord
               .Select((v, i) => GetDataType(values.Select(d => d[i])))
               .ToList();

        if (csv.HeaderRecord is { })
        {
            var index = 0;
            foreach (var header in csv.HeaderRecord)
            {
                var prop = this.GetValidName(header);
                if (string.IsNullOrEmpty(prop))
                    prop = $"Column{index}";

                index++;
            }
        }
        else if (csv.ColumnCount > 0)
        {

        }
        else
            throw new InvalidOperationException($"Cannot read CSV.");

        return types;
    }

    private string GetDataType(IEnumerable<string> columnValues)
    {
        if (!columnValues.Any())
            return "string";

        if (columnValues.Any(x => decimal.TryParse(x, out var _)))
            return "decimal";

        if (columnValues.All(x => double.TryParse(x, out var _)))
            return "double";

        if (columnValues.All(x => DateTime.TryParse(x, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None, out var _)))
            return "DateTime";

        return "string";
    }
}
