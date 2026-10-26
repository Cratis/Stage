// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>An exact ESM pair, not a range expression.</summary>
/// <param name="Language">The compiler's language version value.</param>
/// <param name="Semantics">The compiler's semantic version value.</param>
public sealed record ArtifactRenderVersionPair(LanguageVersion Language, SemanticVersion Semantics);
