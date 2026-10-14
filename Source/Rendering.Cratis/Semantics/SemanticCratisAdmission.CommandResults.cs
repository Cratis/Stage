// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static ArtifactRenderDiagnostic? CommandResultError(SemanticApplicationContext context, SemanticSlice slice, SemanticCommand command)
    {
        if (ResponseNameCollides(context, slice, command))
        {
            return Error(SemanticVersionFeatures.CommandResults, $"Command '{command.Name}' has response names that collide in generated C#.", command.Id);
        }

        if (command.Produces.Any(produced => produced.Mappings.Any(mapping =>
            mapping.Source is SemanticEventContextExpression { Value: not SemanticEventContextValueKind.Occurred })))
        {
            return Error("STAGE-ESM-013", $"Produced event of command '{command.Name}' maps an unsupported command occurrence value ($context); only $context.occurred can be captured at the command boundary.", command.Id);
        }

        return null;
    }

    static bool ResponseNameCollides(SemanticApplicationContext context, SemanticSlice slice, SemanticCommand command)
    {
        if (command.Response is not SemanticRecordCommandResponse response) return false;
        var name = Identifiers.ToPascalCase(command.Name + "Response");
        var names = slice.Commands.Select(candidate => candidate.Name)
            .Concat(slice.Events.Select(candidate => candidate.Name))
            .Concat(slice.ReadModels.Select(candidate => candidate.Name));
        var namespaceName = $"{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(command.Id).Path)}.{Identifiers.ToPascalCase(command.Name)}Response";

        return !GeneratedPascalCase.RecordMembersAreUnique(command.Name + "Response", response.Fields.Select(field => field.Name)) ||
            names.Any(candidate => Identifiers.ToPascalCase(candidate) == name) || context.NamespacePaths.Contains(namespaceName);
    }

    static bool HasCommandOutcome(SemanticCommand command) => !command.Produces.IsEmpty || command.Response is not null;
}
