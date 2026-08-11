// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rules;

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Represents a command defined within a slice.
/// </summary>
/// <param name="Id">The unique identifier of the command.</param>
/// <param name="Name">The name of the command.</param>
/// <param name="Schema">The JSON schema describing the command's input properties.</param>
/// <param name="StateSchema">The JSON schema describing the command's state input (for state-view backed decisions).</param>
/// <param name="Rules">The validation rules defined for the command's properties.</param>
/// <param name="LogicDescription">The natural-language description of the command's logic.</param>
/// <param name="Produces">The events the command appends, in declaration order.</param>
/// <param name="Identifier">The name of the command property whose value identifies the event source the command
/// appends to, or <see langword="null"/> when the command declares none - in which case every execution opens a
/// stream of its own. At most one property can be the identifier.</param>
/// <remarks>
/// Capability added after the record shipped is an <c>init</c> property rather than a trailing parameter of the
/// primary constructor, deliberately. A trailing parameter on a positional record is source compatible and
/// <em>binary</em> breaking: it replaces the constructor and <c>Deconstruct</c> in the compiled signature, so a
/// package built against the previous version fails at run time with a missing method and no compiler error
/// anywhere. Package validation now fails the build on that, and is how this record should grow from here.
/// </remarks>
public record CommandDefinition(
    Guid Id,
    string Name,
    string Schema,
    string StateSchema,
    IReadOnlyList<CommandPropertyRules> Rules,
    string LogicDescription,
    IReadOnlyList<ProducedEvent> Produces,
    string? Identifier = null)
{
    /// <summary>
    /// Gets what the caller must satisfy to execute the command, or <see langword="null"/> when the command
    /// declares no authorization and anyone may execute it.
    /// </summary>
    public AuthorizationRequirement? Authorization { get; init; }

    /// <summary>
    /// Gets the conditions the command as a whole must satisfy — the modeled <c>require</c> rules.
    /// </summary>
    public IReadOnlyList<Requirement> Requirements { get; init; } = [];

    /// <summary>
    /// Gets the read models the command consults before it decides — the modeled <c>reads</c> declarations.
    /// </summary>
    public IReadOnlyList<ReadsDefinition> Reads { get; init; } = [];
}
