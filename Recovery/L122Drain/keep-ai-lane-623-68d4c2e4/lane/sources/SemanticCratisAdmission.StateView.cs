1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: using Cratis.Screenplay.Semantics;
5: using Cratis.Stage.Contracts.Rendering;
6: using Cratis.Stage.Rendering.Cratis.Naming;
7: using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
8: 
9: namespace Cratis.Stage.Rendering.Cratis.Semantics;
10: 
11: /// <summary>
12: /// Admits the supported state-view shape.
13: /// </summary>
14: internal static partial class SemanticCratisAdmission
15: {
16:     static void ValidateStateView(
17:         SemanticApplicationContext context,
18:         SemanticSlice slice,
19:         List<ArtifactRenderDiagnostic> diagnostics)
20:     {
21:         if ((slice.ReadModels.Length == 0 && slice.Reducers.IsEmpty) ||
22:             slice.Projections.Any(projection => !slice.ReadModels.Any(model => model.Id == projection.ReadModel)) ||
23:             slice.Reducers.Any(reducer => !slice.ReadModels.Any(model => model.Id == reducer.ReadModel)) ||
24:             slice.ReadModels.Any(model => slice.Projections.Count(_ => _.ReadModel == model.Id) +
25:                 slice.Reducers.Count(_ => _.ReadModel == model.Id) != 1))
26:         {
27:             diagnostics.Add(Error("STAGE-ESM-007", $"State-view slice '{slice.Name}' needs exactly one projection for each read model.", slice.Id));
28:             return;
29:         }
30: 
31:         foreach (var readModel in slice.ReadModels)
32:         {
33:             var projection = slice.Projections.SingleOrDefault(_ => _.ReadModel == readModel.Id);
34:             if (readModel.Properties.Any(_ => !TypeExists(context, _.Type)) || readModel.Properties.Count(_ => _.IsIdentifier) != 1)
35:             {
36:                 diagnostics.Add(projection is null
37:                     ? Error("STAGE-ESM-008", $"Read model '{readModel.Name}' needs resolvable properties and one identifier.", readModel.Id)
38:                     : Error("STAGE-ESM-008", $"Projection '{projection.Name}' needs a resolvable read model with one identifier.", projection.Id));
39:                 continue;
40:             }
41: 
42:             // Every read model renders as a record with its queries, whether a projection or a reducer
43:             // builds it, and both are persisted through Chronicle's sinks. So these checks run before
44:             // the projection-specific ones below.
45:             if (!ChronicleReadModelPropertyNamesAreSafe.Check(readModel.Properties))
46:             {
47:                 diagnostics.Add(projection is null
48:                     ? Error("STAGE-ESM-017", $"Read model '{readModel.Name}' cannot render: a property name collides with Chronicle's property paths or another generated property.", readModel.Id)
49:                     : Error("STAGE-ESM-017", $"Projection '{projection.Name}' cannot render: a read-model property name collides with Chronicle's property paths or another generated property.", projection.Id));
50:                 continue;
51:             }
52: 
53:             if (!GeneratedPascalCase.ReadModelMembersAreUnique(
54:                 readModel.Name,
55:                 readModel.Properties.Select(property => property.Name),
56:                 slice.Queries.Where(query => query.ReadModel == readModel.Id).Select(query => query.Name)) ||
57:                 !GeneratedPascalCase.QueriesAreUnique(slice.Queries.Where(query => query.ReadModel == readModel.Id)
58:                     .Where(query => query.Argument is null || TypeExists(context, query.Argument.Type))
59:                     .Select(query => (query.Name, query.Argument is null ? string.Empty : new SemanticTypeSystem(context).QueryArgumentType(query.Argument.Type), query.Argument?.Name ?? string.Empty))))
60:             {
61:                 diagnostics.Add(Error("STAGE-ESM-012", $"Read model '{readModel.Name}' has property or query names that collide in generated C#.", readModel.Id));
62:                 continue;
63:             }
64: 
65:             if (projection is null)
66:             {
67:                 continue;
68:             }
69: 
70:             if (!ProjectionReferencedEventNamesAreUnique.Check(projection, context.Events))
71:             {
72:                 diagnostics.Add(Error("STAGE-ESM-012", $"Projection '{projection.Name}' references an event with property names that collide in generated C#.", projection.Id));
73:                 continue;
74:             }
75: 
76:             if (projection.Scope is { } scope)
77:             {
78:                 if (!GeneratedPascalCase.ProjectionTypeNameIsSafe(projection.Name))
79:                 {
80:                     diagnostics.Add(Error("STAGE-ESM-012", $"Projection '{projection.Name}' has a name that collides with its generated C# members.", projection.Id));
81:                     continue;
82:                 }
83: 
84:                 var rejection = SemanticScopedProjectionSupport.Rejection(scope, context, readModel.Properties);
85:                 if (rejection is not null || !projection.Transitions.IsEmpty)
86:                 {
87:                     diagnostics.Add(Error("STAGE-ESM-017", $"Projection '{projection.Name}' cannot render: {rejection ?? "A scope cannot also have flat transitions."}", projection.Id));
88:                 }
89: 
90:                 continue;
91:             }
92: 
93:             if (projection.Transitions.Length != 1)
94:             {
95:                 diagnostics.Add(Error("STAGE-ESM-008", $"Projection '{projection.Name}' does not have one resolvable read-model transition.", projection.Id));
96:                 continue;
97:             }
98: 
99:             var transition = projection.Transitions[0];
100:             if (context.Events.TryGetValue(transition.EventContract, out var sourceEvent) &&
101:                 sourceEvent.Properties.Any(property => !ChronicleEventPropertyNameIsSafe.Check(property.Name)))
102:             {
103:                 diagnostics.Add(Error("STAGE-ESM-017", $"Projection '{projection.Name}' cannot render: an event property name collides with Chronicle expression syntax.", projection.Id));
104:                 continue;
105:             }
106: 
107:             if (!context.Events.TryGetValue(transition.EventContract, out var @event) ||
108:                 transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One ||
109:                 !IsProperty(transition.AffectedInstance.Key, SemanticExpressionRootKind.Event, @event.Properties.Select(_ => _.Id)) ||
110:                 !SemanticFlatProjectionSupport.MappingsMatch(transition.Mappings, readModel.Properties, @event.Properties, SemanticExpressionRootKind.Event) ||
111:                 !UsesEventSourceIdentity(context, transition, @event, readModel))
112:             {
113:                 diagnostics.Add(Error("STAGE-ESM-009", $"Projection '{projection.Name}' cannot preserve its affected instance with model-bound Cratis projection semantics.", projection.Id));
114:             }
115:         }
116: 
117:         foreach (var query in slice.Queries.Where(query => ValidateQueryAuthorization(context, query, diagnostics) && !RefusesNestedValidatedKey(context, query, diagnostics)))
118:         {
119:             var readModel = slice.ReadModels.SingleOrDefault(_ => _.Id == query.ReadModel);
120:             var identifiers = readModel?.Properties.Where(_ => _.IsIdentifier).ToArray() ?? [];
121:             var isSnapshotLookup = query.Cardinality == SemanticQueryCardinality.ZeroOrOne &&
122:                 query.Delivery == SemanticQueryDelivery.Snapshot && identifiers.Length == 1 &&
123:                 query.Argument is not null && query.KeyProperty == identifiers[0].Id && TypeExists(context, query.Argument.Type);
124:             var isLiveCollection = query.Cardinality == SemanticQueryCardinality.Many &&
125:                 query.Delivery == SemanticQueryDelivery.Live && readModel is not null &&
126:                 (query.Argument is null || (TypeExists(context, query.Argument.Type) && readModel.Properties.Any(_ => _.Id == query.KeyProperty)));
127:             if (!isSnapshotLookup && !isLiveCollection)
128:             {
129:                 diagnostics.Add(Error("STAGE-ESM-010", $"Query '{query.Name}' is neither an optional snapshot lookup by identifier nor an observable collection query the renderer can preserve.", query.Id));
130:             }
131:         }
132:     }
133: 
134:     static bool RefusesNestedValidatedKey(SemanticApplicationContext context, SemanticKeyedQuery query, List<ArtifactRenderDiagnostic> diagnostics)
135:     {
136:         if (query.Argument is not { } argument || !IsNested(argument.Type)) return false;
137:         if (new SemanticTypeSystem(context).ValidatedQueryConcept(argument.Type) is not { } concept) return false;
138: 
139:         diagnostics.Add(Error("STAGE-ESM-010", $"Query '{query.Name}' cannot preserve reference lookup semantics for validated concept '{concept.Name}' inside a composite or collection argument.", query.Id));
140:         return true;
141:     }
142: 
143:     static bool IsNested(SemanticTypeReference type) => type.IsCollection || type.Kind == SemanticTypeReferenceKind.CompositeType;
144: }
