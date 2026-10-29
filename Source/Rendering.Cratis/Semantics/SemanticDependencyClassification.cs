// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>A reference classification or an explained absence of an owner reference.</summary>
/// <param name="Kind">The edge category, including reference-carrying containers.</param>
/// <param name="NonReferenceReason">Why no owner reference can occur in this member.</param>
internal sealed record SemanticDependencyClassification(SemanticDependencyKind? Kind, string? NonReferenceReason = null);
