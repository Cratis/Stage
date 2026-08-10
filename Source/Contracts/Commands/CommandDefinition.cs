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
public record CommandDefinition(
    Guid Id,
    string Name,
    string Schema,
    string StateSchema,
    IReadOnlyList<CommandPropertyRules> Rules,
    string LogicDescription,
    IReadOnlyList<ProducedEvent> Produces,
    string? Identifier = null);
