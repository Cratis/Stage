// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Identifies an artifact omitted from a partial plan and the diagnostics that refused it.
/// </summary>
/// <param name="Artifact">The omitted semantic identity.</param>
/// <param name="Kind">The target-neutral artifact kind, such as specification.</param>
/// <param name="Codes">The diagnostic codes explaining the omission.</param>
public sealed record ArtifactRenderRefusal(SemanticId Artifact, string Kind, ImmutableArray<string> Codes);
