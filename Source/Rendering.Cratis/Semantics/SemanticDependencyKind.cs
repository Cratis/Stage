// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Describes why one semantic owner requires another.</summary>
internal enum SemanticDependencyKind
{
    Declaration,
    TypeReference,
    ProducedEvent,
    Projection,
    ReadModelRead,
    Query,
    Route,
    Specification,
    Reaction,
    Capture,
    Policy,
    Constraint,
    TypedContext,
    Expression,
    HistoricalEvent
}
