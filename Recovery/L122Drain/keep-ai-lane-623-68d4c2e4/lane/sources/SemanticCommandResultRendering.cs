1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: using Cratis.Screenplay.Semantics;
5: using Cratis.Stage.Rendering.Cratis.CodeGeneration;
6: using Cratis.Stage.Rendering.Cratis.Naming;
7: 
8: namespace Cratis.Stage.Rendering.Cratis.Semantics;
9: 
10: /// <summary>
11: /// Renders the Handle contract for generated values and command responses.
12: /// </summary>
13: /// <param name="command">The command whose result is rendered.</param>
14: /// <param name="context">The indexed semantic application.</param>
15: internal sealed class SemanticCommandResultRendering(SemanticCommand command, SemanticApplicationContext context)
16: {
17:     readonly SemanticTypeSystem _types = new(context);
18:     readonly SemanticCommandCompliance _compliance = new(context);
19: 
20:     IEnumerable<SemanticProperty> Inputs => command.Properties.Where(property => !property.IsGenerated);
21: 
22:     SemanticProperty? DestinationProperty
23:     {
24:         get
25:         {
26:             var destination = command.Produces.IsEmpty ? null : SemanticDestinations.Of(command, command.Produces[0]) as SemanticResolvedExpression;
27: 
28:             return command.Properties.SingleOrDefault(property => property.Id == destination?.Target);
29:         }
30:     }
31: 
32:     bool HasOccurrence => command.Produces.Any(produced => produced.Mappings.Any(mapping =>
33:         mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred }));
34: 
35:     internal static string PropertyValue(SemanticProperty property) => property.IsGenerated
36:         ? Identifiers.EscapeKeyword(Identifiers.ToCamelCase(property.Name))
37:         : Identifiers.ToPascalCase(property.Name);
38: 
39:     internal void RenderDeclaration(CSharpCodeBuilder builder)
40:     {
41:         var destination = DestinationProperty;
42:         var providesSource = destination is { IsGenerated: false };
43:         var parameters = string.Join(", ", Inputs.Select(property => $"{_compliance.InputAttribute(property.Type)}{_types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"));
44:         var name = Identifiers.ToPascalCase(command.Name);
45:         builder.Attribute(SemanticAuthorizationAttributes.For(command, context))
46:             .OpenBlock($"public record {name}({parameters}){(providesSource ? " : global::Cratis.Chronicle.Events.ICanProvideEventSourceId" : string.Empty)}");
47:         if (providesSource)
48:         {
49:             builder.Line("/// <inheritdoc/>")
50:                 .ExpressionMember("public global::Cratis.Chronicle.Events.EventSourceId GetEventSourceId()", _types.EventSourceExpression(PropertyValue(destination!), destination!.Type))
51:                 .BlankLine();
52:         }
53:     }
54: 
55:     internal void RenderHandle(CSharpCodeBuilder builder, Func<SemanticProducedEvent, string> wrappedEvent, Action renderEventOnly)
56:     {
57:         if (command.Response is null && command.Properties.All(property => !property.IsGenerated))
58:         {
59:             renderEventOnly();
60:             return;
61:         }
62:         var result = HandleResult(wrappedEvent);
63:         builder.OpenBlock($"public {result.Type} Handle({Parameters()})");
64:         if (HasOccurrence)
65:         {
66:             builder.Line($"var occurred = global::{context.RootNamespace}.GeneratedCommands.CommandReceiptTime.OccurredAtReceipt(operation);");
67:         }
68:         builder.Line($"return {result.Value};").EndBlock();
69:     }
70: 
71:     internal void RenderResponseType(CSharpCodeBuilder builder)
72:     {
73:         if (command.Response is not SemanticRecordCommandResponse response) return;
74:         var name = Identifiers.ToPascalCase(command.Name);
75:         var fields = response.Fields.Select(field => $"{_types.Type(field.Type)} {Identifiers.ToPascalCase(field.Name)}");
76:         builder.Summary($"The response from {name}.")
77:             .Line($"public record {name}Response({string.Join(", ", fields)});")
78:             .BlankLine();
79:     }
80: 
81:     string Parameters()
82:     {
83:         var generated = command.Properties.Where(property => property.IsGenerated).Select(property =>
84:             $"[global::Cratis.Arc.Commands.ModelBound.GeneratedValueAttribute({CSharpCodeBuilder.StringLiteral(property.Name)})] {_types.Type(property.Type)} {PropertyValue(property)}");
85: 
86:         return string.Join(", ", generated.Concat(HasOccurrence ? ["global::Cratis.Arc.IOperationContextAccessor operation"] : []));
87:     }
88: 
89:     ResultExpression HandleResult(Func<SemanticProducedEvent, string> wrappedEvent)
90:     {
91:         var events = Events(wrappedEvent);
92:         var response = Response();
93:         if (response is null) return events;
94:         if (command.Produces.IsEmpty) return response;
95: 
96:         return new($"({response.Type}, {events.Type})", $"({response.Value}, {events.Value})");
97:     }
98: 
99:     ResultExpression Events(Func<SemanticProducedEvent, string> wrappedEvent) => command.Produces.Length == 1
100:         ? new("global::Cratis.Chronicle.EventSequences.EventForEventSourceId", wrappedEvent(command.Produces[0]))
101:         : new(
102:             "global::System.Collections.Generic.IEnumerable<global::Cratis.Chronicle.EventSequences.EventForEventSourceId>",
103:             $"new global::Cratis.Chronicle.EventSequences.EventForEventSourceId[] {{ {string.Join(", ", command.Produces.Select(wrappedEvent))} }}");
104: 
105:     ResultExpression? Response() => command.Response switch
106:     {
107:         SemanticScalarCommandResponse scalar => new(_types.Type(scalar.Type), SourceValue(scalar.Source)),
108:         SemanticRecordCommandResponse record => new(
109:             $"{Identifiers.ToPascalCase(command.Name)}Response",
110:             $"new {Identifiers.ToPascalCase(command.Name)}Response({string.Join(", ", record.Fields.Select(field => SourceValue(field.Source)))})"),
111:         _ => null
112:     };
113: 
114:     string SourceValue(SemanticId source) => PropertyValue(command.Properties.Single(property => property.Id == source));
115: 
116:     sealed record ResultExpression(string Type, string Value);
117: }
