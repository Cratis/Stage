1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: using System.Collections.Immutable;
5: using Cratis.Screenplay.Semantics;
6: using Cratis.Stage.Contracts.Rendering;
7: using Cratis.Stage.Rendering.Cratis.Naming;
8: using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
9: 
10: namespace Cratis.Stage.Rendering.Cratis.Semantics;
11: 
12: /// <summary>
13: /// Admits only the ESM subset the first direct Cratis planner renders exactly.
14: /// </summary>
15: internal static partial class SemanticCratisAdmission
16: {
17:     /// <summary>
18:     /// Evaluates target support for the selected semantic scope.
19:     /// </summary>
20:     /// <param name="context">The indexed semantic application.</param>
21:     /// <param name="slices">The selected slices.</param>
22:     /// <returns>Blocking diagnostics for unsupported semantics.</returns>
23:     public static ImmutableArray<ArtifactRenderDiagnostic> Evaluate(
24:         SemanticApplicationContext context,
25:         IReadOnlyList<LocatedSemanticSlice> slices)
26:     {
27:         // Keep the audited surface available in Release as well as in the Debug-only specifications.
28:         _ = SemanticSurfaceLedger.Entries;
29:         var diagnostics = new List<ArtifactRenderDiagnostic>();
30:         if (!PassesPreconditions(context, slices, diagnostics))
31:         {
32:             return [.. diagnostics];
33:         }
34: 
35:         ValidateStrings(context, slices, diagnostics);
36:         ValidateTypes(context, diagnostics);
37:         ValidateCompliance(context, diagnostics);
38:         ValidateConstraints(context, slices, diagnostics);
39:         ValidateGeneratedTypeNames(context, slices, diagnostics);
40: 
41:         var analysedBodies = 0;
42:         foreach (var located in slices)
43:         {
44:             analysedBodies += ValidateSlice(context, located, diagnostics);
45:         }
46: 
47:         if (slices.Any(_ => !_.Slice.Reducers.IsEmpty))
48:         {
49:             diagnostics.Add(new(
50:                 "STAGE-ESM-023",
51:                 ArtifactRenderDiagnosticSeverity.Information,
52:                 $"{analysedBodies} transition bodies analysed.",
53:                 context.Request.Model.Application.Id));
54:         }
55: 
56:         return [.. diagnostics];
57:     }
58: 
59:     /// <summary>
60:     /// Checks referenced localized messages before generating any artifacts.
61:     /// </summary>
62:     /// <param name="context">The semantic application.</param>
63:     /// <param name="slices">The selected slices.</param>
64:     /// <param name="diagnostics">The blocking diagnostics.</param>
65:     internal static void ValidateStrings(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
66:     {
67:         if (context.Strings is null)
68:         {
69:             return;
70:         }
71: 
72:         var messages = context.Application.Concepts.SelectMany(concept => concept.Validations.Select(rule => (rule.Message, concept.Id)))
73:             .Concat(slices.SelectMany(located => located.Slice.Commands.SelectMany(command =>
74:                 command.Validations.Select(rule => (rule.Message, command.Id))
75:                     .Concat(command.Requirements.Select(requirement => (requirement.Message, command.Id))))));
76:         foreach (var (message, artifact) in messages.Where(_ => _.Message?.StartsWith("$strings.", StringComparison.Ordinal) == true))
77:         {
78:             ValidateStringKey(context.Strings, message!, artifact, diagnostics);
79:         }
80:     }
81: 
82:     /// <summary>
83:     /// Checks whether a semantic type refers to a supported, resolved type.
84:     /// </summary>
85:     /// <param name="context">The indexed semantic application.</param>
86:     /// <param name="type">The declared type.</param>
87:     /// <returns>Whether the declared type exists.</returns>
88:     /// <exception cref="UnsupportedSemanticRendering">The type reference kind is not handled.</exception>
89:     internal static bool TypeExists(SemanticApplicationContext context, SemanticTypeReference type) => type.Kind switch
90:     {
91:         SemanticTypeReferenceKind.Primitive => type.Primitive != SemanticPrimitiveType.Unknown,
92:         SemanticTypeReferenceKind.Concept => context.Concepts.ContainsKey(type.Target),
93:         SemanticTypeReferenceKind.CompositeType => context.Types.ContainsKey(type.Target),
94:         SemanticTypeReferenceKind.Unknown => false,
95:         _ => throw UnsupportedSemanticRendering.For(nameof(SemanticTypeReferenceKind), type.Kind)
96:     };
97: 
98:     internal static bool UsesOpaquePolicies(SemanticApplicationContext context, IEnumerable<LocatedSemanticSlice> slices) =>
99:         slices.SelectMany(located => located.Slice.Commands.Select(command => command.Authorization)
100:                 .Concat(located.Slice.Queries.Select(query => query.Authorization)))
101:             .OfType<SemanticAuthorization>()
102:             .Any(authorization => OpaquePolicies(authorization, context.Application.Policies).Any());
103: 
104:     static bool PassesPreconditions(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
105:     {
106:         var model = context.Request.Model;
107:         if (!SemanticVersionFeatures.Supports(model))
108:         {
109:             diagnostics.Add(Error("STAGE-ESM-016", "The model's language/semantic version is not one the Cratis ESM planner has audited.", model.Application.Id));
110:             return false;
111:         }
112: 
113:         diagnostics.AddRange(SemanticVersionFeatureAdmission.Verify(context, slices));
114:         return diagnostics.Count == 0 &&
115:             Passes(diagnostics, () => ValidateEventRevisions(context, slices, diagnostics)) &&
116:             Passes(diagnostics, () => ValidateReservedNamespace(context, slices, diagnostics)) &&
117:             Passes(diagnostics, () => ValidateEventSourceRoutes(context, slices, diagnostics)) &&
118:             Passes(diagnostics, () => ValidateWholeNumberLiterals(context, slices, diagnostics));
119:     }
120: 
121:     static bool Passes(List<ArtifactRenderDiagnostic> diagnostics, Action validate)
122:     {
123:         validate();
124:         return diagnostics.Count == 0;
125:     }
126: 
127:     static void ValidateReservedNamespace(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
128:     {
129:         // TypedContexts is reserved for reducer and policy runtime tokens and wrappers. A modeled
130:         // namespace with the same root path can rebind TenantId or Identity in unrelated artifacts.
131:         if ((slices.Any(_ => !_.Slice.Reducers.IsEmpty) || UsesOpaquePolicies(context, slices)) && DeclaresTypedContexts(context))
132:         {
133:             diagnostics.Add(Error("STAGE-ESM-022", "Generated namespace 'TypedContexts' shadows the reserved reducer and policy runtime namespace.", context.Request.Model.Application.Id));
134:         }
135:     }
136: 
137:     static bool DeclaresTypedContexts(SemanticApplicationContext context) =>
138:         context.NamespacePaths.Any(path => path == $"{context.RootNamespace}.TypedContexts" || path == context.TypedContextsNamespace);
139: 
140:     static void ValidateGeneratedTypeNames(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
141:     {
142:         foreach (var (artifact, kind, name) in GeneratedTypeNames.Collisions(
143:             context.Application,
144:             slices.Select(located => ((IEnumerable<string>)located.Path, located.Slice)),
145:             SelectedConstraints(context, slices).Select(selected =>
146:                 ((IEnumerable<string>)context.Slice(selected.Slice.Id).Path, selected.Slice.Id, selected.Constraint)),
147:             rendersStringsCatalog: context.Strings is not null,
148:             opaquePolicyTypes: SemanticPolicyContextRuntime.GeneratedTypes(context, slices),
149:             commonNamespace: string.Join('.', context.Domain.Append("Common")),
150:             eventSources: SemanticEventSourceArtifactRenderer.Selected(context, slices),
151:             eventSourcesNamespace: string.Join('.', context.Domain.Append("EventSources")),
152:             policiesNamespace: string.Join('.', context.Domain.Append("GeneratedPolicies"))))
153:         {
154:             var message = kind == "Namespace"
155:                 ? $"Generated type '{name}' collides with a generated C# namespace."
156:                 : $"{kind} '{name}' collides with another generated C# type in the same namespace.";
157:             diagnostics.Add(Error("STAGE-ESM-012", message, artifact));
158:         }
159:     }
160: 
161:     static int ValidateSlice(SemanticApplicationContext context, LocatedSemanticSlice located, List<ArtifactRenderDiagnostic> diagnostics)
162:     {
163:         foreach (var @event in located.Slice.Events.Where(@event =>
164:             !GeneratedPascalCase.EventMembersAreUnique(@event.Name, @event.Properties.Select(property => property.Name))))
165:         {
166:             diagnostics.Add(Error("STAGE-ESM-012", $"Event '{@event.Name}' has property names that collide in generated C#.", @event.Id));
167:         }
168: 
169:         var analysedBodies = ValidateReducers(context, located.Slice, diagnostics);
170:         ValidateSliceKind(context, located.Slice, diagnostics);
171:         ValidateCallerFixtures(located.Slice, diagnostics);
172:         SemanticSpecificationAdmission.Validate(context, located.Slice, diagnostics);
173: 
174:         return analysedBodies;
175:     }
176: 
177:     static void ValidateSliceKind(SemanticApplicationContext context, SemanticSlice slice, List<ArtifactRenderDiagnostic> diagnostics)
178:     {
179:         switch (slice.Kind)
180:         {
181:             case SemanticSliceKind.StateChange:
182:                 ValidateStateChange(context, slice, diagnostics);
183:                 break;
184:             case SemanticSliceKind.Automation:
185:                 ValidateAutomation(context, slice, diagnostics);
186:                 break;
187:             case SemanticSliceKind.StateView:
188:                 ValidateStateView(context, slice, diagnostics);
189:                 break;
190:             default:
191:                 diagnostics.Add(Error("STAGE-ESM-001", "The slice kind is not supported by the Cratis ESM planner.", slice.Id));
192:                 break;
193:         }
194:     }
195: 
196:     static void ValidateStringKey(StringsCatalogInput.Catalog strings, string message, SemanticId artifact, List<ArtifactRenderDiagnostic> diagnostics)
197:     {
198:         var key = message["$strings.".Length..];
199:         if (!strings.Locales[strings.DefaultLocale].ContainsKey(key))
200:         {
201:             diagnostics.Add(Error("STAGE-ESM-018", $"String key '{message}' is missing from the default locale '{strings.DefaultLocale}'.", artifact));
202:             return;
203:         }
204: 
205:         if (strings.Locales.Values.Any(locale => locale.TryGetValue(key, out var value) &&
206:             value.Any(IsUnpreservable)))
207:         {
208:             diagnostics.Add(Error("STAGE-ESM-018", $"String key '{message}' contains formatting or control characters the Cratis validator cannot preserve.", artifact));
209:         }
210:     }
211: 
212:     static bool IsUnpreservable(char character) => character is '{' or '}' || char.IsControl(character) || char.IsSurrogate(character);
213: 
214:     static void ValidateEventSourceRoutes(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices, List<ArtifactRenderDiagnostic> diagnostics)
215:     {
216:         foreach (var source in SemanticEventSourceArtifactRenderer.Selected(context, slices))
217:         {
218:             ValidateReservedNames(source, diagnostics);
219:         }
220: 
221:         foreach (var specification in slices.SelectMany(_ => _.Slice.Specifications))
222:         {
223:             ValidateFixtureRoutes(context, specification, diagnostics);
224:         }
225:     }
226: 
227:     static void ValidateReservedNames(SemanticEventSource source, List<ArtifactRenderDiagnostic> diagnostics)
228:     {
229:         if (source.SourceKind == "Default")
230:         {
231:             diagnostics.Add(Error("STAGE-ESM-030", "The stored event source name 'Default' is reserved by Chronicle.", source.Id));
232:         }
233: 
234:         foreach (var stream in source.Streams.Where(stream => stream.StreamKind == "All"))
235:         {
236:             diagnostics.Add(Error("STAGE-ESM-030", "The stored event stream name 'All' is reserved by Chronicle.", stream.Id));
237:         }
238:     }
239: 
240:     static void ValidateFixtureRoutes(SemanticApplicationContext context, SemanticSpecification specification, List<ArtifactRenderDiagnostic> diagnostics)
241:     {
242:         var fixtures = specification.GivenEvents.Concat(specification.ThenEvents).Where(occurrence => occurrence.Route is not null).ToArray();
243:         if (fixtures.Any(occurrence => !SemanticSpecificationRouteRendering.CanFormat(occurrence.Route!, context)))
244:         {
245:             diagnostics.Add(Error("STAGE-ESM-030", $"Specification '{specification.Name}' has a fixture route whose stream identity cannot be formatted portably.", specification.Id));
246:         }
247: 
248:         // Companion scenarios replay actual command routes, even if a then-event leaves its route unspecified.
249:         if ((!specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty) &&
250:             SemanticSpecificationRouteRendering.ForCommand(specification, context) is { } commandRoute &&
251:             !SemanticSpecificationRouteRendering.CanFormat(commandRoute, context))
252:         {
253:             diagnostics.Add(Error("STAGE-ESM-030", $"Specification '{specification.Name}' has a command replay route whose stream identity cannot be formatted portably.", specification.Id));
254:         }
255:     }
256: 
257:     static bool IsProperty(SemanticExpression? expression, SemanticExpressionRootKind root, IEnumerable<SemanticId> candidates) =>
258:         expression is SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property } resolved &&
259:         resolved.Root == root && candidates.Contains(resolved.Target);
260: 
261:     static ArtifactRenderDiagnostic Error(string code, string message, SemanticId artifact) =>
262:         new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact);
263: }
