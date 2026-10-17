// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Carries the property access context for a rendered condition.
/// </summary>
/// <param name="Properties">The command or triggering-event properties.</param>
/// <param name="Receiver">The default property receiver.</param>
/// <param name="Application">The indexed application.</param>
/// <param name="Accessor">An optional custom property accessor.</param>
/// <param name="Reads">The protected command reads that read-backed operands resolve against.</param>
internal sealed record SemanticConditionRenderingContext(
    IReadOnlyList<SemanticProperty> Properties,
    string Receiver,
    SemanticApplicationContext Application,
    Func<SemanticProperty, string>? Accessor = null,
    SemanticReadScope? Reads = null);
