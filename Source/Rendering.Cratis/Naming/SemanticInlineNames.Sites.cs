// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Naming;

internal sealed partial class SemanticInlineNames
{
    internal static SemanticInlineNames Create(SemanticApplicationContext context)
    {
        var sites = new List<SemanticInlineNameSite>();
        var reservations = new List<SemanticInlineNameReservation>();
        foreach (var command in context.Commands.Values.Where(command => command.Handler is not null))
        {
            var scope = $"command:{command.Id}";
            var typeScope = $"command-types:{command.Id}";
            var name = Identifiers.ToPascalCase(command.Name);
            sites.Add(new(Key("handler-body", command.Id), $"Handle{name}", command.Handler!.RequirementId, scope));
            sites.Add(new(Key("handler-context", command.Id), $"{name}Context", command.Handler.RequirementId, scope, typeScope));
            sites.Add(new(Key("handler-command", command.Id), $"{name}Command", command.Handler.RequirementId, scope, typeScope));
            reservations.AddRange(command.Properties.Select(property => new SemanticInlineNameReservation(scope, context.MemberNames.Property(property))));
            reservations.AddRange(GeneratedPascalCase.RecordMembers.Append(name).Concat(["Handle", "Provide", "GetEventSourceId"])
                .Select(member => new SemanticInlineNameReservation(scope, member)));

            // Nested helper types must not shadow the model types visible to the authored body.
            var namespaces = command.Produces.Select(produced => SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(produced.EventContract).Path))
                .Append(context.CommonNamespace)
                .Append(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(command.Id).Path))
                .ToHashSet(StringComparer.Ordinal);
            reservations.AddRange(context.GeneratedTypes.Where(type => namespaces.Contains(type.Namespace))
                .Select(type => new SemanticInlineNameReservation(typeScope, type.Name)));
        }
        foreach (var concept in context.Concepts.Values.Where(concept => SemanticGenerationRendering.IsRule(concept.Generation)))
        {
            var scope = $"concept:{concept.Id}";
            var name = Identifiers.ToPascalCase(concept.Name);
            sites.Add(new(Key("generation-body", concept.Id), $"Generate{name}", concept.Generation!.RequirementId!, scope));
            reservations.AddRange(GeneratedPascalCase.RecordMembers.Concat([name, "Value", "New", "NotSet"])
                .Select(member => new SemanticInlineNameReservation(scope, member)));
        }
        return new(sites, reservations);
    }
}
