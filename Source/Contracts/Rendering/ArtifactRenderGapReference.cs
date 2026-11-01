// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Identifies one root or intermediate cause, not just an owner that may have multiple independent causes.</summary>
/// <param name="Owner">The owning semantic identity.</param>
/// <param name="Code">The exact public degradation code on that owner.</param>
public sealed record ArtifactRenderGapReference(SemanticId Owner, string Code);
