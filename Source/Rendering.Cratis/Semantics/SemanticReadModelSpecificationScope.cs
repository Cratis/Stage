// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Carries the shared source-rendering state for one read-model specification.
/// </summary>
/// <param name="Builder">The specification source builder.</param>
/// <param name="Types">The specification type and value renderer.</param>
/// <param name="Context">The indexed semantic application.</param>
/// <param name="Located">The specification's declaring slice.</param>
internal sealed record SemanticReadModelSpecificationScope(
    CSharpCodeBuilder Builder,
    SemanticTypeSystem Types,
    SemanticApplicationContext Context,
    LocatedSemanticSlice Located);
