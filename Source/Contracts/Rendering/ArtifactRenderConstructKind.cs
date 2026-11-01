// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>Semantic leaf categories, never a rule for inferring graph leaves or counting containers.</summary>
public enum ArtifactRenderConstructKind
{
    /// <summary>An explicitly unknown semantic owner, conservatively scored and visible.</summary>
    Unknown,

    /// <summary>A command.</summary>
    Command,

    /// <summary>An operation.</summary>
    Operation,

    /// <summary>A guard.</summary>
    Guard,

    /// <summary>A validation.</summary>
    Validation,

    /// <summary>A policy.</summary>
    Policy,

    /// <summary>An event.</summary>
    Event,

    /// <summary>A read model.</summary>
    ReadModel,

    /// <summary>A concept.</summary>
    Concept,

    /// <summary>A composite type.</summary>
    CompositeType,

    /// <summary>A query.</summary>
    Query,

    /// <summary>A projection.</summary>
    Projection,

    /// <summary>A reducer.</summary>
    Reducer,

    /// <summary>A reaction.</summary>
    Reaction,

    /// <summary>A capture.</summary>
    Capture,

    /// <summary>A constraint.</summary>
    Constraint,

    /// <summary>A screen.</summary>
    Screen,

    /// <summary>A specification.</summary>
    Specification
}
