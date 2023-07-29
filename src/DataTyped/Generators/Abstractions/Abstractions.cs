using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace DataTyped.Generator;

public abstract class Transformer<TInput, TOutput> where TInput : SyntaxNode
{
    public virtual bool Filter(SyntaxNode node, CancellationToken cancellationToken) => node is TInput;

    public abstract TOutput Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken);
}

public interface Visitor<TInput, TOutput>
{
    void Visit(TInput input, TOutput output);
}

public interface Renderer<TInput>
{
    void Render(SourceProductionContext context, TInput result);
}

public abstract class Aggregator<TInput, TResult> :
    Transformer<TInput, GeneratorSyntaxContext>,
    Visitor<GeneratorSyntaxContext, TResult>
    where TInput : SyntaxNode
{
    public override GeneratorSyntaxContext Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken) => context;

    public virtual void Aggregate(ImmutableArray<GeneratorSyntaxContext> data, TResult output)
    {
        foreach (var item in data)
        {
            if (item.Node is TInput input)
            {
                Visit(item, output);
            }
        }
    }

    public abstract void Visit(GeneratorSyntaxContext context, TResult output);
}

public abstract class Generator<TInput, TResult> :
    Transformer<TInput, GeneratorSyntaxContext>,
    Visitor<GeneratorSyntaxContext, TResult>,
    Renderer<TResult>
    where TInput : SyntaxNode
{
    public override GeneratorSyntaxContext Transform(GeneratorSyntaxContext context, CancellationToken cancellationToken) => context;

    public virtual void Visit(ImmutableArray<GeneratorSyntaxContext> data, TResult output)
    {
        foreach (var item in data)
        {
            if (item.Node is TInput input)
            {
                Visit(item, output);
            }
        }
    }

    public abstract void Visit(GeneratorSyntaxContext context, TResult output);

    public abstract void Render(SourceProductionContext context, TResult result);
}