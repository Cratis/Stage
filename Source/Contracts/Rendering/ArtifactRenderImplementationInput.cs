// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Records one implementation requirement that was an input to planning. Prerelease: input provenance, outside the output-only digest.
/// </summary>
/// <param name="RequirementId">The requirement's stable identity.</param>
/// <param name="Role">The role the implementation plays.</param>
/// <param name="Owner">The semantic address that owns the implementation.</param>
/// <param name="Language">The declared implementation language, when any.</param>
/// <param name="File">The declared implementation file, when any.</param>
/// <param name="ContentHash">The declared content hash, or an empty string when the file could not be resolved.</param>
public sealed record ArtifactRenderImplementationInput(
    string RequirementId,
    SemanticImplementationRole Role,
    SemanticAddress Owner,
    string? Language,
    string? File,
    string ContentHash);
