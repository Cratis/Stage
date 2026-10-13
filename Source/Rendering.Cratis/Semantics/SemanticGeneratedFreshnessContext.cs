// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Carries the event selection and destination guards for freshness rendering.
/// </summary>
/// <param name="Command">The command whose events are rendered.</param>
/// <param name="Application">The indexed application.</param>
/// <param name="WrappedEvent">Renders one event wrapper.</param>
/// <param name="SelectedEvents">An optional expression for the selected events.</param>
/// <param name="Inclusion">An optional destination inclusion predicate.</param>
internal sealed record SemanticGeneratedFreshnessContext(
    SemanticCommand Command,
    SemanticApplicationContext Application,
    Func<SemanticProducedEvent, string> WrappedEvent,
    string? SelectedEvents = null,
    Func<SemanticProperty, string>? Inclusion = null);
