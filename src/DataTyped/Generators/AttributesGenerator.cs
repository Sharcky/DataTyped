using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace DataTyped.Generator;

public class AttributesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput((i) => i.AddSource("DataTyped.g.cs", SourceText.From(GeneratedAttributes.Text, Encoding.UTF8)));
    }
}

