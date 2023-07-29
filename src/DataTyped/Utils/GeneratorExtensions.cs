using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DataTyped.Generator;

public static class GeneratorExtensions
{
    public static void AddSourceFile(this SourceProductionContext context, string identifier, string fileContent)
    {
        if (string.IsNullOrEmpty(fileContent))
            return;

        context.AddSource($"{identifier}.g.cs", SourceText.From(fileContent, Encoding.UTF8));
    }

    public static void ReportException(this SourceProductionContext context, Exception ex)
    {
        // Report a diagnostic if an exception occurs while generating code; allows consumers to know what is going on
        string message = $"Exception: {ex}";
        context.ReportDiagnostic(Diagnostic.Create(
            new DiagnosticDescriptor(
                "DT000",
                message,
                message,
                "DataTyped",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true),
            Location.None));
    }

    private static Func<GeneratorSyntaxContext, CancellationToken, GeneratorSyntaxContext> ContextSelector = (ctx, c) => ctx;

    public static IncrementalValuesProvider<GeneratorSyntaxContext> OfType<T>(this SyntaxValueProvider provider, Predicate<T>? filter = null) where T : SyntaxNode
    {
        Func<SyntaxNode, CancellationToken, bool> predicate =
            filter is null
            ? static (s, c) => s is T node
            : (s, c) => s is T node && filter(node);

        return provider.CreateSyntaxProvider(predicate, ContextSelector);
    }

    public static IncrementalValueProvider<ImmutableArray<T>> Merge<T>(this IncrementalValuesProvider<T> left, IncrementalValuesProvider<T> right)
    {
        return
            left.Collect()
                .Combine(right.Collect())
                .Select((tuple, token) => ImmutableArray.CreateRange(tuple.Left.Concat(tuple.Right)));
    }

    public static IncrementalValuesProvider<TResult> Apply<T, TResult>(this IncrementalGeneratorInitializationContext context, Transformer<T, TResult> visitor) where T : SyntaxNode
    {
        return context.SyntaxProvider.CreateSyntaxProvider(visitor.Filter, visitor.Transform);
    }

    public static void Register<TInput, TIntermediate, TOutput, TGenerator>(this IncrementalGeneratorInitializationContext context, TGenerator visitor) 
        where TInput : SyntaxNode
        where TGenerator: Transformer<TInput, TIntermediate>, Visitor<TIntermediate, TOutput>, Renderer<TOutput>
        where TOutput: new()
    {
        var all = context.Apply(visitor).Collect();
        context.RegisterSourceOutput(all, (spc, data) =>
        {
            var output = new TOutput();
            foreach (var d in data)
            {
                visitor.Visit(d, output);
            }

            visitor.Render(spc, output);
        });
    }

    public static IncrementalValueProvider<(T1, T2, T3)> Flatten<T1, T2, T3>(this IncrementalValueProvider<((T1, T2), T3)> source) =>
        source.Select((tuple, _) => (tuple.Item1.Item1, tuple.Item1.Item2, tuple.Item2));
    
}
