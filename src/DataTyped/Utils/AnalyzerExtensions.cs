using Microsoft.CodeAnalysis.Diagnostics;

namespace DataTyped.Generator;

public static class AnalyzerExtensions
{
    public static bool IsGenerated(this SyntaxNodeAnalysisContext context) =>
        context.IsGeneratedCode || context.Node.IsGenerated();
}
