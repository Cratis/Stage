// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// One <see langword="provide"/> declaration with the model level it was written at.
/// </summary>
/// <param name="Scope">The module, feature and slice names above and including the declaring level; empty for the application.</param>
/// <param name="Value">The provided value.</param>
internal sealed record SemanticProvidedDeclaration(IReadOnlyList<string> Scope, SemanticProvidedValue Value);

/// <summary>
/// Walks the values a model provides to its request context (Cratis/Stage#1116).
/// </summary>
internal static class SemanticProvidedValues
{
    /// <summary>
    /// Lists every provided value in the application, outermost level first.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <returns>The declarations in model order.</returns>
    internal static IReadOnlyList<SemanticProvidedDeclaration> Declared(SemanticApplicationContext context)
    {
        var declarations = new List<SemanticProvidedDeclaration>();
        Add(declarations, [], context.Application.Provides);
        foreach (var module in context.Application.Modules)
        {
            Add(declarations, [module.Name], module.Provides);
            Walk(declarations, [module.Name], module.Features);
        }

        return declarations;
    }

    static void Walk(List<SemanticProvidedDeclaration> declarations, string[] scope, IEnumerable<SemanticFeature> features)
    {
        foreach (var feature in features)
        {
            string[] featureScope = [.. scope, feature.Name];
            Add(declarations, featureScope, feature.Provides);
            foreach (var slice in feature.Slices) Add(declarations, [.. featureScope, slice.Name], slice.Provides);
            Walk(declarations, featureScope, feature.Features);
        }
    }

    static void Add(List<SemanticProvidedDeclaration> declarations, string[] scope, ImmutableArray<SemanticProvidedValue> provides)
    {
        if (provides.IsDefault) return;
        foreach (var provided in provides) declarations.Add(new(scope, provided));
    }
}
