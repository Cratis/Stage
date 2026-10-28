// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Identifies one planner component that contributed to a plan. Prerelease: recorded as input provenance, outside the output-only digest.
/// </summary>
/// <param name="Kind">The component kind.</param>
/// <param name="Name">The component name, unique within its kind.</param>
/// <param name="Version">The exact component version.</param>
/// <param name="Digest">The optional lowercase SHA-256 of the component content, or <see langword="null"/> when it has none.</param>
public sealed record ArtifactRenderComponent(ArtifactRenderComponentKind Kind, string Name, string Version, string? Digest = null);
