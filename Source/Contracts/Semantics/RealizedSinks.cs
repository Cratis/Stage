// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// Represents a named sink the model declares, and the kind of thing it stores.
/// </summary>
/// <param name="Name">The name of the sink, as the model declares it.</param>
/// <param name="Kind">What the sink stores.</param>
public sealed record RealizedSink(string Name, SemanticSinkKind Kind);

/// <summary>
/// Represents the named sinks of an application: the sinks the model declares, and the read models and graphs that choose one.
/// </summary>
/// <remarks>
/// Sinks are realization metadata, like passivity: they are outside the portable executable model, so choosing or dropping
/// one leaves the model's revision unchanged. A read model or graph that names no sink uses the default sink of its kind.
/// </remarks>
/// <param name="Declarations">The declared sinks, in declaration order.</param>
/// <param name="ReadModels">The sink each read model that names one stores to, by read model.</param>
/// <param name="Graphs">The sink each graph that names one stores to, by graph name.</param>
public sealed record RealizedSinks(
    ImmutableArray<RealizedSink> Declarations,
    ImmutableDictionary<SemanticId, string> ReadModels,
    ImmutableDictionary<string, string> Graphs)
{
    /// <summary>
    /// Gets the sinks of an application that declares none.
    /// </summary>
    public static RealizedSinks None { get; } = new([], ImmutableDictionary.Create<SemanticId, string>(), ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.Ordinal));

    /// <summary>
    /// Gets whether the application declares no sink and no read model or graph chooses one.
    /// </summary>
    public bool IsEmpty => Declarations.IsDefaultOrEmpty && ReadModels.IsEmpty && Graphs.IsEmpty;

    /// <summary>
    /// Derives the sinks of a semantic compilation.
    /// </summary>
    /// <param name="compilation">The <see cref="SemanticCompilation"/>.</param>
    /// <returns>The <see cref="RealizedSinks"/>; <see cref="None"/> when the model declares none.</returns>
    public static RealizedSinks From(SemanticCompilation compilation)
    {
        var declarations = compilation.Sinks.IsDefault ? [] : compilation.Sinks;
        if (declarations.IsEmpty && compilation.ReadModelSinks.IsEmpty && compilation.GraphSinks.IsEmpty)
        {
            return None;
        }

        return new(
            [.. declarations.Select(sink => new RealizedSink(sink.Name, sink.Kind))],
            compilation.ReadModelSinks,
            compilation.GraphSinks.WithComparers(StringComparer.Ordinal));
    }

    /// <summary>
    /// Gets the sink a read model stores to.
    /// </summary>
    /// <param name="readModel">The read model.</param>
    /// <returns>The sink name, or <see langword="null"/> when the read model uses the default sink.</returns>
    public string? ForReadModel(SemanticId readModel) => ReadModels.TryGetValue(readModel, out var sink) ? sink : null;

    /// <summary>
    /// Gets the sink a graph stores to.
    /// </summary>
    /// <param name="graph">The graph name.</param>
    /// <returns>The sink name, or <see langword="null"/> when the graph uses the default sink.</returns>
    public string? ForGraph(string graph) => Graphs.TryGetValue(graph, out var sink) ? sink : null;
}
