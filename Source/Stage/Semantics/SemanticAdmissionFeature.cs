// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Semantics;

/// <summary>
/// A precise refusal for a modeled version-specific construct.
/// </summary>
/// <param name="Artifact">The semantic identity.</param>
/// <param name="Kind">The artifact kind.</param>
/// <param name="Capability">The missing capability.</param>
/// <param name="Details">The diagnostic code and reason.</param>
public sealed record SemanticAdmissionFeature(string Artifact, string Kind, string Capability, string Details);
