// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts a Screenplay <see cref="CommandSyntax"/> into a Stage <see cref="CommandDefinition"/>. The command's
/// input schema is synthesized from its typed properties; the state schema is an empty object (Screenplay has no
/// state schema concept) and the logic description is empty.
/// </summary>
public static class CommandConverter
{
    /// <summary>
    /// Converts a command declaration into its Stage definition.
    /// </summary>
    /// <param name="command">The command to convert.</param>
    /// <param name="schema">The schema synthesizer.</param>
    /// <param name="slicePath">The fully-qualified slice path, used to derive a stable identifier.</param>
    /// <returns>The Stage command definition.</returns>
    public static CommandDefinition Convert(CommandSyntax command, SchemaSynthesizer schema, string slicePath) =>
        new(
            DeterministicId.From($"{slicePath}.command.{command.Name}"),
            command.Name,
            schema.ForProperties(command.Properties),
            SchemaSynthesizer.EmptyObjectSchema,
            ValidationRuleConverter.Convert(command.Validations),
            string.Empty);
}
