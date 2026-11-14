1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: using Cratis.Screenplay.Semantics;
5: using Cratis.Stage.Contracts.Rendering;
6: using Cratis.Stage.Rendering.Cratis.Naming;
7: 
8: namespace Cratis.Stage.Rendering.Cratis.Semantics;
9: 
10: /// <summary>
11: /// Admits the supported state-change shape.
12: /// </summary>
13: internal static partial class SemanticCratisAdmission
14: {
15:     static void ValidateStateChange(
16:         SemanticApplicationContext context,
17:         SemanticSlice slice,
18:         List<ArtifactRenderDiagnostic> diagnostics)
19:     {
20:         if (slice.Commands.Length != 1)
21:         {
22:             diagnostics.Add(Error("STAGE-ESM-004", $"State-change slice '{slice.Name}' must contain exactly one command.", slice.Id));
23:             return;
24:         }
25: 
26:         ValidateCommand(context, slice, slice.Commands[0], diagnostics);
27:     }
28: 
29:     static void ValidateCommand(
30:         SemanticApplicationContext context,
31:         SemanticSlice slice,
32:         SemanticCommand command,
33:         List<ArtifactRenderDiagnostic> diagnostics)
34:     {
35:         if (!GeneratedPascalCase.CommandMembersAreUnique(command.Name, command.Properties.Select(property => property.Name)))
36:         {
37:             diagnostics.Add(Error("STAGE-ESM-012", $"Command '{command.Name}' has property names that collide in generated C#.", command.Id));
38:             return;
39:         }
40: 
41:         if (!ValidateCommandAuthorization(context, command, diagnostics))
42:         {
43:             return;
44:         }
45: 
46:         if (!command.CodeValidations.IsEmpty || command.Validations.Any(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation))
47:         {
48:             var ids = command.CodeValidations.Select(validation => validation.RequirementId)
49:                 .Concat(command.Validations.Where(rule => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation)
50:                     .Select(rule => rule.RequirementId));
51:             diagnostics.Add(Error("STAGE-ESM-005", $"Command '{command.Name}' contains validation implementation bodies ({string.Join(", ", ids)}): Arc does not supply RuleContext.Occurred (received-at) to a generated validator, and Stage does not yet enforce the pure capability.", command.Id));
52:             return;
53:         }
54: 
55:         if (slice.Events.Any(@event =>
56:                 @event.Properties.Any(property => !TypeExists(context, property.Type) || property.Type.IsOptional)) ||
57:             command.Properties.Any(_ => !TypeExists(context, _.Type)) ||
58:             !command.Validations.All(rule => SemanticValidationRendering.CanRender(rule, context)) ||
59:             command.Validations.Any(rule => command.Properties.Single(property => property.Id == rule.Property).Type is
60:             { Kind: SemanticTypeReferenceKind.Concept, IsOptional: true } type &&
61:                 context.Concepts[type.Target].Primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean) ||
62:             !command.Requirements.All(_ => SemanticRequirementRendering.CanRender(_, command, context)) ||
63:             (!command.Requirements.IsEmpty && command.Properties.Any(_ => HasValidatedConcept(context, _.Type, []))) ||
64:             command.Validations.Any(_ =>
65:             {
66:                 var property = command.Properties.Single(p => p.Id == _.Property);
67:                 return property.Type.Kind == SemanticTypeReferenceKind.Concept && !context.Concepts[property.Type.Target].Values.IsEmpty;
68:             }) ||
69:             command.Properties.Any(_ =>
70:                 (_.Type.Kind == SemanticTypeReferenceKind.Concept && _.Type.IsOptional &&
71:                  !context.Concepts[_.Type.Target].Validations.IsEmpty) ||
72:                 (_.Type.Kind == SemanticTypeReferenceKind.CompositeType && HasOptionalRequiredConcept(context, _.Type, []))) ||
73:             !HasCommandOutcome(command))
74:         {
75:             diagnostics.Add(Error("STAGE-ESM-005", $"Command '{command.Name}' exceeds the first Cratis command capability.", command.Id));
76:             return;
77:         }
78: 
79:         if (CommandResultError(context, slice, command) is { } resultError)
80:         {
81:             diagnostics.Add(resultError);
82:             return;
83:         }
84: 
85:         if (command.Produces.Any(produced =>
86:             !context.Events.TryGetValue(produced.EventContract, out var @event) || produced.Condition is not null ||
87:             produced.When is not null ||
88:             @event.Tags.Concat(produced.Tags).Distinct(StringComparer.Ordinal).Count() != @event.Tags.Length + produced.Tags.Length ||
89:             !IsProperty(SemanticDestinations.Of(command, produced), SemanticExpressionRootKind.Command, command.Properties.Where(_ => _.IsIdentifier).Select(_ => _.Id)) ||
90:             @event.Properties.Any(_ => !TypeExists(context, _.Type) || _.Type.IsOptional) ||
91:             produced.Mappings.Length != @event.Properties.Length ||
92:             !@event.Properties.All(property => produced.Mappings.Any(mapping => mapping.TargetProperty == property.Id &&
93:                 (IsProperty(mapping.Source, SemanticExpressionRootKind.Command, command.Properties.Select(_ => _.Id)) ||
94:                  (mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } occurrence && occurrence.Type == property.Type) ||
95:                  mapping.Source is SemanticValueExpression)))))
96:         {
97:             diagnostics.Add(Error("STAGE-ESM-006", $"Produced event of command '{command.Name}' cannot be rendered without changing its destination or mappings.", command.Id));
98:         }
99:     }
100: 
101:     static bool HasValidatedConcept(SemanticApplicationContext context, SemanticTypeReference type, HashSet<SemanticId> visited) =>
102:         type.Kind switch
103:         {
104:             SemanticTypeReferenceKind.Concept => !context.Concepts[type.Target].Validations.IsEmpty,
105:             SemanticTypeReferenceKind.CompositeType when visited.Add(type.Target) =>
106:                 context.Types[type.Target].Properties.Any(_ => HasValidatedConcept(context, _.Type, visited)),
107:             _ => false
108:         };
109: 
110:     static bool HasOptionalRequiredConcept(SemanticApplicationContext context, SemanticTypeReference type, HashSet<SemanticId> visited)
111:     {
112:         if (type.Kind != SemanticTypeReferenceKind.CompositeType || !visited.Add(type.Target))
113:         {
114:             return false;
115:         }
116: 
117:         return context.Types[type.Target].Properties.Any(property =>
118:             (property.Type.Kind == SemanticTypeReferenceKind.Concept && property.Type.IsOptional &&
119:              !context.Concepts[property.Type.Target].Validations.IsEmpty) ||
120:             HasOptionalRequiredConcept(context, property.Type, visited));
121:     }
122: }
