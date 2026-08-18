// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Concepts;
using Cratis.Stage.Contracts.Policies;

namespace Cratis.Stage.Contracts;

/// <summary>
/// Represents the root of an event model — the structure the engine runs.
/// </summary>
/// <param name="Id">The unique identifier of the event model.</param>
/// <param name="Name">The name of the event model.</param>
/// <param name="Collections">The module collections within the event model.</param>
/// <remarks>
/// Capability added after the record shipped is an <c>init</c> property rather than a trailing parameter of the
/// primary constructor, deliberately. A trailing parameter on a positional record is source compatible and
/// <em>binary</em> breaking: it replaces the constructor and <c>Deconstruct</c> in the compiled signature, so a
/// package built against the previous version fails at run time with a missing method and no compiler error
/// anywhere. Package validation fails the build on that, and is how this record should grow from here.
/// </remarks>
public record EventModel(
    Guid Id,
    string Name,
    IReadOnlyList<ModuleCollection> Collections)
{
    /// <summary>
    /// Gets the concepts the application declares — the strongly typed domain values everything else is written
    /// in terms of.
    /// </summary>
    /// <remarks>
    /// Declared once for the whole application rather than per slice, which is where the language puts them.
    /// </remarks>
    public IReadOnlyList<ConceptDefinition> Concepts { get; init; } = [];

    /// <summary>
    /// Gets the policies the application declares — what the names a command's <c>authorize</c> declaration
    /// refers to actually check.
    /// </summary>
    public IReadOnlyList<PolicyDefinition> Policies { get; init; } = [];
}
