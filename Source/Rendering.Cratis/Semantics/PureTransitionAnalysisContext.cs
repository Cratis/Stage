// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Microsoft.CodeAnalysis;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Keeps the binding environment and compilation-specific concept trust together during a purity walk.
/// </summary>
/// <param name="Descriptor">The typed context contract for the body.</param>
/// <param name="RootNamespace">The generated application's root namespace.</param>
/// <param name="GeneratedConcepts">The generated concept symbols trusted in this compilation.</param>
internal sealed record PureTransitionAnalysisContext(
    SemanticTypedContextDescriptor Descriptor,
    string RootNamespace,
    IReadOnlyDictionary<INamedTypeSymbol, ITypeSymbol> GeneratedConcepts);
