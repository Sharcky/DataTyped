using Microsoft.CodeAnalysis;

namespace DataTyped.Generator;

[Generator]
public class MainGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        new AttributesGenerator().Initialize(context);
        new AdditionalFilesGenerator().Initialize(context);
        new TypesGenerator().Initialize(context);
    }
}

