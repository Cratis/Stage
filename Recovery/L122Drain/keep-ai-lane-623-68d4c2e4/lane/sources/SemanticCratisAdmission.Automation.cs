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
11: /// Admits event-triggered Automation slices without dropping their own artifacts.
12: /// </summary>
13: internal static partial class SemanticCratisAdmission
14: {
15:     static void ValidateAutomation(SemanticApplicationContext context, SemanticSlice slice, List<ArtifactRenderDiagnostic> diagnostics)
16:     {
17:         foreach (var command in slice.Commands)
18:         {
19:             ValidateCommand(context, slice, command, diagnostics);
20:         }
21:         if (SemanticReadSurface.IsDeclaredBy(slice))
22:         {
23:             ValidateStateView(context, slice, diagnostics);
24:         }
25:         foreach (var reaction in slice.Reactions.IsDefault ? [] : slice.Reactions)
26:         {
27:             ValidateReaction(context, slice, reaction, diagnostics);
28:         }
29:         if (slice.Events.Any(@event => !ReactionEventIsRenderable(context, @event)))
30:         {
31:             diagnostics.Add(Error("STAGE-ESM-039", $"Automation slice '{slice.Name}' declares event properties the Cratis target cannot preserve.", slice.Id));
32:         }
33:     }
34: 
35:     static void ValidateReaction(SemanticApplicationContext context, SemanticSlice slice, SemanticReaction reaction, List<ArtifactRenderDiagnostic> diagnostics)
36:     {
37:         if (reaction.Triggers.IsEmpty)
38:         {
39:             diagnostics.Add(Error("STAGE-ESM-039", $"Reaction '{reaction.Name}' has no event subscription for a Chronicle reactor.", reaction.Id));
40:             return;
41:         }
42:         if (ReactionNamesCollide(context, slice, reaction))
43:         {
44:             diagnostics.Add(Error("STAGE-ESM-012", $"Reaction '{reaction.Name}' has event handler names that collide in generated C#.", reaction.Id));
45:         }
46:         foreach (var trigger in reaction.Triggers.Where(trigger => !ReactionTriggerIsRenderable(context, trigger)))
47:         {
48:             diagnostics.Add(Error("STAGE-ESM-039", $"Reaction '{reaction.Name}' cannot be rendered without changing its condition, destination or mappings.", reaction.Id));
49:         }
50:     }
51: 
52:     static bool ReactionNamesCollide(SemanticApplicationContext context, SemanticSlice slice, SemanticReaction reaction) =>
53:         SliceNaming.FileName(slice.Name) == $"{GeneratedPascalCase.From(reaction.Name)}.cs" ||
54:         ReactionHandlerMatchesClass(context, reaction) || ReactionHandlerNamesCollide(context, reaction);
55: 
56:     static bool ReactionHandlerMatchesClass(SemanticApplicationContext context, SemanticReaction reaction) =>
57:         reaction.Triggers.GroupBy(trigger => trigger.Source).Any(group =>
58:             GeneratedPascalCase.From(context.Events[group.Key].Name) == GeneratedPascalCase.From(reaction.Name));
59: 
60:     static bool ReactionHandlerNamesCollide(SemanticApplicationContext context, SemanticReaction reaction) =>
61:         reaction.Triggers.Select(trigger => trigger.Source).Distinct().Select(id => GeneratedPascalCase.From(context.Events[id].Name))
62:             .GroupBy(name => name, StringComparer.Ordinal).Any(group => group.Count() > 1);
63: 
64:     static bool ReactionTriggerIsRenderable(SemanticApplicationContext context, SemanticReactionTrigger trigger)
65:     {
66:         var source = context.Events[trigger.Source];
67: 
68:         return ReactionConditionIsRenderable(context, source, trigger.Where) &&
69:             trigger.Produces.All(produced => CanRenderProduction(context, source, produced)) &&
70:             trigger.Invokes.All(invocation => CanRenderInvocation(context, source, invocation));
71:     }
72: 
73:     static bool ReactionConditionIsRenderable(SemanticApplicationContext context, SemanticEventContract source, SemanticCondition? condition) =>
74:         condition is null || SemanticConditionRendering.CanRender(condition, source.Properties, context, numericOrderingOnly: true);
75: 
76:     static bool CanRenderProduction(SemanticApplicationContext context, SemanticEventContract source, SemanticProducedEvent produced)
77:     {
78:         if (!context.Events.TryGetValue(produced.EventContract, out var target)) return false;
79: 
80:         return HasPortableReactionProduction(produced) && ReactionEventIsRenderable(context, target) &&
81:             ReactionMappingCountMatches(target, produced) && ReactionTagsAreDistinct(target, produced) &&
82:             ReactionMappingsMatch(context, source, target, produced) && ReactionDestinationMatches(context, source, produced);
83:     }
84: 
85:     static bool HasPortableReactionProduction(SemanticProducedEvent produced) =>
86:         produced.Condition is null && produced.When is null && produced.Route is null;
87: 
88:     static bool ReactionEventIsRenderable(SemanticApplicationContext context, SemanticEventContract @event) =>
89:         @event.Properties.All(property => TypeExists(context, property.Type) && !property.Type.IsOptional);
90: 
91:     static bool ReactionMappingCountMatches(SemanticEventContract target, SemanticProducedEvent produced) =>
92:         produced.Mappings.Length == target.Properties.Length;
93: 
94:     static bool ReactionTagsAreDistinct(SemanticEventContract target, SemanticProducedEvent produced) =>
95:         target.Tags.Concat(produced.Tags).Distinct(StringComparer.Ordinal).Count() == target.Tags.Length + produced.Tags.Length;
96: 
97:     static bool ReactionMappingsMatch(SemanticApplicationContext context, SemanticEventContract source, SemanticEventContract target, SemanticProducedEvent produced) =>
98:         target.Properties.All(property => produced.Mappings.Any(mapping => mapping.TargetProperty == property.Id &&
99:             ReactionExpressionMatches(context, source, mapping.Source, property.Type)));
100: 
101:     static bool ReactionDestinationMatches(SemanticApplicationContext context, SemanticEventContract source, SemanticProducedEvent produced) =>
102:         produced.Destination is null || (produced.DestinationType is { IsCollection: false, IsOptional: false } type &&
103:             ReactionExpressionMatches(context, source, produced.Destination, type));
104: 
105:     static bool CanRenderInvocation(SemanticApplicationContext context, SemanticEventContract source, SemanticInvocation invocation)
106:     {
107:         if (!context.Commands.TryGetValue(invocation.Command, out var command)) return false;
108: 
109:         return command.Properties.Where(property => !property.IsGenerated).All(property =>
110:             invocation.Mappings.FirstOrDefault(mapping => mapping.TargetProperty == property.Id) is { } mapping
111:                 ? ReactionExpressionMatches(context, source, mapping.Source, property.Type)
112:                 : property.Type.IsOptional);
113:     }
114: 
115:     static bool ReactionExpressionMatches(SemanticApplicationContext context, SemanticEventContract source, SemanticExpression expression, SemanticTypeReference target) =>
116:         TypeExists(context, target) && expression switch
117:         {
118:             SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } resolved =>
119:                 source.Properties.Any(property => property.Id == resolved.Target && property.Type == target),
120:             SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } occurrence => occurrence.Type == target,
121:             SemanticValueExpression value => ReactionValueMatches(context, value.Value, target),
122:             _ => false
123:         };
124: 
125:     static bool ReactionValueMatches(SemanticApplicationContext context, SemanticValue value, SemanticTypeReference type)
126:     {
127:         if (value is SemanticNullValue) return type.IsOptional;
128:         if (type.IsCollection) return ReactionCollectionValueMatches(context, value, type);
129:         if (type.Kind == SemanticTypeReferenceKind.CompositeType) return ReactionCompositeValueMatches(context, value, type);
130:         if (IsReactionEnumeration(context, type)) return ReactionEnumerationValueMatches(context, value, type);
131: 
132:         return ReactionPrimitiveValueMatches(context, value, SemanticValidationRendering.UnderlyingPrimitive(type, context));
133:     }
134: 
135:     static bool ReactionCollectionValueMatches(SemanticApplicationContext context, SemanticValue value, SemanticTypeReference type) =>
136:         value is SemanticArrayValue array && array.Values.All(element => ReactionValueMatches(context, element, type with { IsCollection = false }));
137: 
138:     static bool ReactionCompositeValueMatches(SemanticApplicationContext context, SemanticValue value, SemanticTypeReference type) =>
139:         value is SemanticCompositeValue composite &&
140:         context.Types[type.Target].Properties.All(property => composite.Properties.Any(item => item.TargetProperty == property.Id && ReactionValueMatches(context, item.Value, property.Type)));
141: 
142:     static bool IsReactionEnumeration(SemanticApplicationContext context, SemanticTypeReference type) =>
143:         type.Kind == SemanticTypeReferenceKind.Concept && !context.Concepts[type.Target].Values.IsEmpty;
144: 
145:     static bool ReactionEnumerationValueMatches(SemanticApplicationContext context, SemanticValue value, SemanticTypeReference type) =>
146:         value is SemanticTextValue member && context.Concepts[type.Target].Values.Contains(member.Value, StringComparer.Ordinal);
147: 
148:     static bool ReactionPrimitiveValueMatches(SemanticApplicationContext context, SemanticValue value, SemanticPrimitiveType primitive) => (value, primitive) switch
149:     {
150:         (SemanticTextValue, SemanticPrimitiveType.Text) => true,
151:         (SemanticTextValue text, SemanticPrimitiveType.Uuid) => Guid.TryParse(text.Value, out _),
152:         (SemanticTextValue text, SemanticPrimitiveType.Date) => DateOnly.TryParse(text.Value, System.Globalization.CultureInfo.InvariantCulture, out _),
153:         (SemanticTextValue text, SemanticPrimitiveType.DateTime) => DateTimeOffset.TryParse(text.Value, System.Globalization.CultureInfo.InvariantCulture, out _),
154:         (SemanticNumberValue number, SemanticPrimitiveType.WholeNumber) => ReactionWholeNumberValueMatches(context, number),
155:         (SemanticNumberValue, SemanticPrimitiveType.DecimalNumber) => true,
156:         (SemanticBooleanValue, SemanticPrimitiveType.Boolean) => true,
157:         _ => false
158:     };
159: 
160:     static bool ReactionWholeNumberValueMatches(SemanticApplicationContext context, SemanticNumberValue number) =>
161:         number.Value == decimal.Truncate(number.Value) && ReactionWholeNumberIsInRange(context, number.Value);
162: 
163:     static bool ReactionWholeNumberIsInRange(SemanticApplicationContext context, decimal value) =>
164:         context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V8)
165:             ? value is >= long.MinValue and <= long.MaxValue
166:             : value is >= int.MinValue and <= int.MaxValue;
167: }
