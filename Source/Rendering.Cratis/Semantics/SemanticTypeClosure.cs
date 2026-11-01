// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Accumulates the concepts and composite types reachable from the declarations it is shown, including transitive dependencies.
/// </summary>
/// <param name="context">The indexed application.</param>
internal sealed class SemanticTypeClosure(SemanticApplicationContext context)
{
    /// <summary>
    /// Gets the reachable concepts.
    /// </summary>
    public HashSet<SemanticId> Concepts { get; } = [];

    /// <summary>
    /// Gets the reachable composite types.
    /// </summary>
    public HashSet<SemanticId> Types { get; } = [];

    /// <summary>
    /// Visits every property type.
    /// </summary>
    /// <param name="properties">The properties to visit.</param>
    public void Properties(IEnumerable<SemanticProperty> properties)
    {
        foreach (var property in properties) Reference(property.Type);
    }

    /// <summary>
    /// Visits one type reference.
    /// </summary>
    /// <param name="reference">The reference, if any.</param>
    public void Reference(SemanticTypeReference? reference)
    {
        if (reference?.Kind == SemanticTypeReferenceKind.Concept) Concepts.Add(reference.Target);
        if (reference?.Kind == SemanticTypeReferenceKind.CompositeType) Composite(reference.Target);
    }

    /// <summary>
    /// Visits one composite type and the types its properties reference.
    /// </summary>
    /// <param name="id">The composite type identity.</param>
    public void Composite(SemanticId id)
    {
        if (Types.Add(id)) Properties(context.Types[id].Properties);
    }
}
