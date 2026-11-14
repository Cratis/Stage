1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: using System.Collections.Immutable;
5: using Cratis.Screenplay.Semantics;
6: using Cratis.Stage.Contracts.Rendering;
7: 
8: namespace Cratis.Stage.Rendering.Cratis.Semantics;
9: 
10: internal static partial class SemanticCratisAdmission
11: {
12:     static void ValidateCompliance(SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
13:     {
14:         foreach (var concept in context.Application.Concepts.Where(concept => concept.Compliance is not null).Where(concept => !concept.Values.IsEmpty))
15:         {
16:             diagnostics.Add(Error(SemanticVersionFeatures.ProtectedIdentity, $"Concept '{concept.Name}' has compliance on an enumeration; its protection cannot be rendered.", concept.Id));
17:         }
18: 
19:         var protectedUses = IdentityUses(context)
20:             .Select(identity => (Concept: ProtectedConcept(context, identity.Type), identity.Use))
21:             .Where(identity => identity.Concept is not null);
22:         foreach (var (concept, use) in protectedUses)
23:         {
24:             diagnostics.Add(Error(SemanticVersionFeatures.ProtectedIdentity, $"Protected concept '{concept!.Name}' is used as {use}. Use a surrogate identity and keep the protected value as a separate property.", concept.Id));
25:         }
26:     }
27: 
28:     static SemanticConcept? ProtectedConcept(SemanticApplicationContext context, SemanticTypeReference type)
29:     {
30:         if (type.Kind != SemanticTypeReferenceKind.Concept) return null;
31:         if (!context.Concepts.TryGetValue(type.Target, out var concept)) return null;
32: 
33:         return concept.Compliance is not null ? concept : null;
34:     }
35: 
36:     static IEnumerable<(SemanticTypeReference Type, string Use)> IdentityUses(SemanticApplicationContext context)
37:     {
38:         var slices = SemanticVersionFeatures.Slices(context.Application).ToArray();
39:         var properties = slices.SelectMany(slice => slice.Commands.SelectMany(command => command.Properties)
40:             .Concat(slice.ReadModels.SelectMany(model => model.Properties)));
41:         foreach (var property in properties.Where(property => property.IsIdentifier))
42:         {
43:             yield return (property.Type, $"identifier '{property.Name}'");
44:         }
45: 
46:         foreach (var use in CommandDestinations(slices)) yield return use;
47:         foreach (var use in ReactionDestinations(context.Application)) yield return use;
48:         foreach (var use in EventSourceIdentities(context.Application)) yield return use;
49:         foreach (var use in ProjectionIdentities(context, slices)) yield return use;
50:     }
51: 
52:     static IEnumerable<(SemanticTypeReference Type, string Use)> CommandDestinations(IEnumerable<SemanticSlice> slices) =>
53:         slices.SelectMany(slice => slice.Commands).SelectMany(CommandDestinations);
54: 
55:     static IEnumerable<(SemanticTypeReference Type, string Use)> CommandDestinations(SemanticCommand command) =>
56:         command.Produces.Select(produced => SemanticDestinations.Of(command, produced)).OfType<SemanticResolvedExpression>()
57:             .Select(destination => command.Properties.SingleOrDefault(property => property.Id == destination.Target))
58:             .OfType<SemanticProperty>()
59:             .Select(property => (property.Type, $"command destination '{command.Name}.{property.Name}'"));
60: 
61:     static IEnumerable<(SemanticTypeReference Type, string Use)> ReactionDestinations(SemanticApplication application) =>
62:         SemanticReactionProductions.In(application)
63:             .Where(production => production.Produced.Destination is not null)
64:             .SelectMany(production => TypedIdentity(production.Produced.DestinationType, $"reaction destination '{production.Reaction.Name}'"));
65: 
66:     static IEnumerable<(SemanticTypeReference Type, string Use)> EventSourceIdentities(SemanticApplication application) =>
67:         application.EventSources.SelectMany(source => TypedIdentity(source.IdentifierType, $"event source identifier '{source.Name}'")
68:             .Concat(source.Streams.SelectMany(stream => StreamIdentities(source.Name, stream))));
69: 
70:     static IEnumerable<(SemanticTypeReference Type, string Use)> StreamIdentities(string source, SemanticEventStream stream) =>
71:         TypedIdentity(stream.StreamIdType, $"stream identity '{source}.{stream.Name}'")
72:             .Concat(stream.StreamIdParts.Select(part => (part.Type, $"stream identity part '{source}.{stream.Name}.{part.Name}'")));
73: 
74:     static IEnumerable<(SemanticTypeReference Type, string Use)> TypedIdentity(SemanticTypeReference? type, string use) =>
75:         type is null ? [] : [(type, use)];
76: 
77:     static IEnumerable<(SemanticTypeReference Type, string Use)> ProjectionIdentities(SemanticApplicationContext context, IEnumerable<SemanticSlice> slices) =>
78:         slices.SelectMany(slice => slice.Projections).SelectMany(projection =>
79:             projection.Transitions.Select(transition => FlatProjectionKeyType(context, transition)).OfType<SemanticTypeReference>()
80:                 .Select(type => (type, $"projection key '{projection.Name}'"))
81:                 .Concat(ScopedProjectionKeyTypes(context, projection.Scope).Select(type => (type, $"projection or removal key '{projection.Name}'")))
82:                 .Concat(ProjectionMemberIdentities(context, projection.Scope, context.ReadModels.TryGetValue(projection.ReadModel, out var model) ? model.Properties : [])));
83: 
84:     static IEnumerable<(SemanticTypeReference Type, string Use)> ProjectionMemberIdentities(
85:         SemanticApplicationContext context,
86:         SemanticProjectionScope? scope,
87:         IEnumerable<SemanticProperty> properties)
88:     {
89:         if (scope is null) return [];
90: 
91:         return scope.Joins.SelectMany(join => PropertyIdentity(properties, join.On, "projection join identity"))
92:             .Concat(scope.Children.SelectMany(child => ChildMemberIdentities(context, child, properties)))
93:             .Concat(scope.Nested.SelectMany(nested => ProjectionMemberIdentities(context, nested.Scope, ProjectionMemberProperties(context, properties, nested.Property))));
94:     }
95: 
96:     static IEnumerable<(SemanticTypeReference Type, string Use)> ChildMemberIdentities(
97:         SemanticApplicationContext context,
98:         SemanticProjectionChildren child,
99:         IEnumerable<SemanticProperty> properties)
100:     {
101:         var members = ProjectionMemberProperties(context, properties, child.Property);
102: 
103:         return PropertyIdentity(members, child.IdentifiedBy, "projection child identity")
104:             .Concat(ProjectionMemberIdentities(context, child.Scope, members));
105:     }
106: 
107:     static IEnumerable<(SemanticTypeReference Type, string Use)> PropertyIdentity(IEnumerable<SemanticProperty> properties, SemanticId id, string use) =>
108:         TypedIdentity(properties.SingleOrDefault(property => property.Id == id)?.Type, use);
109: 
110:     static ImmutableArray<SemanticProperty> ProjectionMemberProperties(SemanticApplicationContext context, IEnumerable<SemanticProperty> properties, SemanticId id)
111:     {
112:         var property = properties.SingleOrDefault(property => property.Id == id);
113: 
114:         return property is not null && context.Types.TryGetValue(property.Type.Target, out var type) ? type.Properties : [];
115:     }
116: 
117:     static SemanticTypeReference? FlatProjectionKeyType(SemanticApplicationContext context, SemanticProjectionTransition transition)
118:     {
119:         if (transition.AffectedInstance.Key is not SemanticResolvedExpression key) return null;
120:         if (!context.Events.TryGetValue(transition.EventContract, out var @event)) return null;
121: 
122:         return @event.Properties.SingleOrDefault(property => property.Id == key.Target)?.Type;
123:     }
124: 
125:     static IEnumerable<SemanticTypeReference> ScopedProjectionKeyTypes(SemanticApplicationContext context, SemanticProjectionScope? scope) =>
126:         scope is null ? [] : ProjectionKeys(scope)
127:             .SelectMany(identity => KeyPaths(identity.Key).Select(path => ProjectionKeyType(context, identity.Contract, path)))
128:             .OfType<SemanticTypeReference>();
129: 
130:     static IEnumerable<(SemanticId Contract, SemanticProjectionKey Key)> ProjectionKeys(SemanticProjectionScope scope) =>
131:         scope.From.SelectMany(from => Keys(from.EventContract, from.Key, from.ParentKey))
132:             .Concat(scope.Removals.SelectMany(removal => Keys(removal.EventContract, removal.Key, removal.ParentKey)))
133:             .Concat(scope.Joins.Where(join => join.Key is not null).Select(join => (join.EventContract, join.Key!)))
134:             .Concat(scope.JoinRemovals.Select(removal => (removal.EventContract, removal.Key)))
135:             .Concat(scope.Children.SelectMany(child => ProjectionKeys(child.Scope)))
136:             .Concat(scope.Nested.SelectMany(nested => ProjectionKeys(nested.Scope)));
137: 
138:     static IEnumerable<(SemanticId Contract, SemanticProjectionKey Key)> Keys(SemanticId contract, SemanticProjectionKey key, SemanticProjectionKey? parent) =>
139:         parent is null ? [(contract, key)] : [(contract, key), (contract, parent)];
140: 
141:     static IEnumerable<IEnumerable<SemanticId>> KeyPaths(SemanticProjectionKey key) => key switch
142:     {
143:         SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property } => [property.Path],
144:         SemanticProjectionCompositeKey composite => composite.Parts.Select(part => part.Value).OfType<SemanticProjectionEventProperty>().Select(property => property.Path.AsEnumerable()),
145:         _ => []
146:     };
147: 
148:     static SemanticTypeReference? ProjectionKeyType(SemanticApplicationContext context, SemanticId contract, IEnumerable<SemanticId> path)
149:     {
150:         if (!context.Events.TryGetValue(contract, out var @event)) return null;
151:         var properties = @event.Properties;
152:         SemanticProperty? property = null;
153:         foreach (var id in path)
154:         {
155:             property = properties.SingleOrDefault(candidate => candidate.Id == id);
156:             if (property is null) return null;
157:             properties = context.Types.TryGetValue(property.Type.Target, out var composite) ? composite.Properties : [];
158:         }
159: 
160:         return property?.Type;
161:     }
162: }
