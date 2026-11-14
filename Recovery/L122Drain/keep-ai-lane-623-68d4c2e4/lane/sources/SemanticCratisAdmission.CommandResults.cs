1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: using Cratis.Screenplay.Semantics;
5: using Cratis.Stage.Contracts.Rendering;
6: using Cratis.Stage.Rendering.Cratis.Naming;
7: 
8: namespace Cratis.Stage.Rendering.Cratis.Semantics;
9: 
10: internal static partial class SemanticCratisAdmission
11: {
12:     static ArtifactRenderDiagnostic? CommandResultError(SemanticApplicationContext context, SemanticSlice slice, SemanticCommand command)
13:     {
14:         if (MapsGeneratedConstraint(context, command))
15:         {
16:             return Error(SemanticVersionFeatures.CommandResults, $"Command '{command.Name}' maps a generated value into a unique constraint; Stage cannot preserve input validation for that combination yet.", command.Id);
17:         }
18:         if (ResponseNameCollides(context, slice, command))
19:         {
20:             return Error(SemanticVersionFeatures.CommandResults, $"Command '{command.Name}' has response names that collide in generated C#.", command.Id);
21:         }
22: 
23:         if (command.Produces.Any(produced => produced.Mappings.Any(mapping =>
24:             mapping.Source is SemanticEventContextExpression { Value: not SemanticEventContextValueKind.Occurred })))
25:         {
26:             return Error("STAGE-ESM-013", $"Produced event of command '{command.Name}' maps an unsupported command occurrence value ($context); only $context.occurred can be captured at the command boundary.", command.Id);
27:         }
28: 
29:         return null;
30:     }
31: 
32:     static bool MapsGeneratedConstraint(SemanticApplicationContext context, SemanticCommand command)
33:     {
34:         var generated = command.Properties.Where(property => property.IsGenerated).Select(property => property.Id).ToHashSet();
35:         var targets = context.Constraints.SelectMany(selected => selected.Constraint.Targets);
36: 
37:         return targets.Any(target => command.Produces.Where(produced => produced.EventContract == target.EventContract)
38:             .SelectMany(produced => produced.Mappings)
39:             .Any(mapping => target.Properties.Contains(mapping.TargetProperty) && UsesGeneratedValue(mapping.Source, generated)));
40:     }
41: 
42:     static bool UsesGeneratedValue(SemanticExpression expression, HashSet<SemanticId> generated) =>
43:         expression is SemanticResolvedExpression source && generated.Contains(source.Target);
44: 
45:     static bool ResponseNameCollides(SemanticApplicationContext context, SemanticSlice slice, SemanticCommand command)
46:     {
47:         if (command.Response is not SemanticRecordCommandResponse response) return false;
48:         var name = Identifiers.ToPascalCase(command.Name + "Response");
49:         var names = slice.Commands.Select(candidate => candidate.Name)
50:             .Concat(slice.Events.Select(candidate => candidate.Name))
51:             .Concat(slice.ReadModels.Select(candidate => candidate.Name));
52:         var namespaceName = $"{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(command.Id).Path)}.{Identifiers.ToPascalCase(command.Name)}Response";
53: 
54:         return !GeneratedPascalCase.RecordMembersAreUnique(command.Name + "Response", response.Fields.Select(field => field.Name)) ||
55:             names.Any(candidate => Identifiers.ToPascalCase(candidate) == name) || context.NamespacePaths.Contains(namespaceName);
56:     }
57: 
58:     static bool HasCommandOutcome(SemanticCommand command) => !command.Produces.IsEmpty || command.Response is not null;
59: }
