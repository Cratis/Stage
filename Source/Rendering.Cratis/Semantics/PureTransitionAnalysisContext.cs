// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Keeps the binding environment and compilation-specific concept trust together during a purity walk.
/// </summary>
internal sealed record PureTransitionAnalysisContext
{
    /// <summary>Gets the typed context contract for the body.</summary>
    public required SemanticTypedContextDescriptor Descriptor { get; init; }

    /// <summary>Gets the generated application's root namespace.</summary>
    public required string RootNamespace { get; init; }

    /// <summary>Gets the generated concept symbols trusted in this compilation.</summary>
    public required IReadOnlyDictionary<INamedTypeSymbol, ITypeSymbol> GeneratedConcepts { get; init; }

    /// <summary>Gets the full names of exactly the allocated wrappers and local shapes this body may bind to.</summary>
    public required IReadOnlySet<string> ContextTypes { get; init; }

    /// <summary>Gets the compilation together with the exact context identity, for type trust checks.</summary>
    /// <param name="compilation">The analysis compilation.</param>
    /// <returns>The trust scope.</returns>
    internal TrustScope Scope(Compilation compilation) => new(compilation, ContextTypes);
}

/// <summary>The analysis compilation and the exact typed-context identity that decides whether a generated type is trusted data.</summary>
/// <param name="Compilation">The analysis compilation.</param>
/// <param name="ContextTypes">The full names of the allocated wrappers and local shapes.</param>
internal sealed record TrustScope(Compilation Compilation, IReadOnlySet<string> ContextTypes);
