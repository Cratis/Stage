1: // Copyright (c) Cratis. All rights reserved.
2: // Licensed under the MIT license. See LICENSE file in the project root for full license information.
3: 
4: using Cratis.Screenplay.Semantics;
5: using Cratis.Stage.Rendering.Cratis.CodeGeneration;
6: using Cratis.Stage.Rendering.Cratis.Naming;
7: using Cratis.Stage.Rendering.Cratis.Renderers;
8: 
9: namespace Cratis.Stage.Rendering.Cratis.Semantics;
10: 
11: /// <summary>
12: /// Renders application concepts and composite types directly from ESM.
13: /// </summary>
14: internal static class SemanticCommonArtifactRenderer
15: {
16:     /// <summary>
17:     /// Renders one concept.
18:     /// </summary>
19:     /// <param name="concept">The semantic concept.</param>
20:     /// <param name="context">The indexed semantic application.</param>
21:     /// <returns>The generated source artifact.</returns>
22:     public static RenderedFile Render(SemanticConcept concept, SemanticApplicationContext context)
23:     {
24:         var name = Identifiers.ToPascalCase(concept.Name);
25:         var builder = new CSharpCodeBuilder().Namespace(context.CommonNamespace);
26:         if (!concept.Values.IsEmpty)
27:         {
28:             RenderEnum(builder, concept, name);
29:         }
30:         else
31:         {
32:             RenderConcept(builder, concept, name, context);
33:         }
34: 
35:         return new(Path.Combine(context.CommonFolder, $"{name}.cs"), builder.ToString()) { Sources = [concept.Id] };
36:     }
37: 
38:     /// <summary>
39:     /// Renders one composite type.
40:     /// </summary>
41:     /// <param name="type">The semantic composite type.</param>
42:     /// <param name="context">The indexed semantic application.</param>
43:     /// <returns>The generated source artifact.</returns>
44:     public static RenderedFile Render(SemanticCompositeType type, SemanticApplicationContext context)
45:     {
46:         var types = new SemanticTypeSystem(context);
47:         var name = Identifiers.ToPascalCase(type.Name);
48:         var parameters = string.Join(", ", type.Properties.Select(_ => $"{types.Type(_.Type)} {Identifiers.ToPascalCase(_.Name)}"));
49:         var builder = new CSharpCodeBuilder()
50:             .Namespace(context.CommonNamespace);
51:         context.Docs(type.Id).Render(builder, $"Represents {Identifiers.ToWords(type.Name)}.")
52:             .Line($"public record {name}({parameters});");
53:         return new(Path.Combine(context.CommonFolder, $"{name}.cs"), builder.ToString()) { Sources = [type.Id] };
54:     }
55: 
56:     static void RenderEnum(CSharpCodeBuilder builder, SemanticConcept concept, string name)
57:     {
58:         builder.Summary($"Represents the possible values of {name}.").OpenBlock($"public enum {name}");
59:         foreach (var member in SemanticEnumMembers.Declarations(concept))
60:         {
61:             builder.Line($"{member},");
62:         }
63: 
64:         builder.EndBlock();
65:     }
66: 
67:     static void RenderConcept(CSharpCodeBuilder builder, SemanticConcept concept, string name, SemanticApplicationContext context)
68:     {
69:         var types = new SemanticTypeSystem(context);
70:         var isIdentifier = context.IdentifierConcepts.Contains(concept.Id);
71:         var primitive = types.Primitive(concept.Primitive);
72:         builder.Using("Cratis.Concepts");
73:         if (isIdentifier)
74:         {
75:             builder.Using("Cratis.Chronicle.Events");
76:         }
77: 
78:         var baseType = isIdentifier ? $"global::Cratis.Chronicle.Events.EventSourceId<{primitive}>" : $"global::Cratis.Concepts.ConceptAs<{primitive}>";
79:         var generationInterface = GenerationInterface(concept, context);
80:         builder.Summary($"Represents {Identifiers.ToWords(concept.Name)}.");
81:         ConceptComplianceRendering.Render(builder, concept.Compliance);
82:         builder.OpenBlock($"public record {name}({primitive} Value) : {baseType}(Value){generationInterface}")
83:             .Line($"public static readonly {name} NotSet = new({types.NotSet(concept.Primitive)});")
84:             .BlankLine();
85: 
86:         RenderFactory(builder, concept, context);
87:         if (!isIdentifier)
88:         {
89:             builder.Line($"public static implicit operator {primitive}({name} value) => value.Value;");
90:         }
91: 
92:         builder.Line($"public static implicit operator {name}({primitive} value) => new(value);").EndBlock();
93:         RenderValidator(builder, concept, name, context.RootNamespace);
94:     }
95: 
96:     static bool NeedsFactory(SemanticConcept concept, SemanticApplicationContext context) =>
97:         context.IdentifierConcepts.Contains(concept.Id) || context.Commands.Values.Any(command =>
98:             command.Properties.Any(property => property.IsGenerated && property.Type.Target == concept.Id));
99: 
100:     static bool IsGeneratable(SemanticConcept concept, SemanticApplicationContext context) =>
101:         concept.Primitive == SemanticPrimitiveType.Uuid && NeedsFactory(concept, context);
102: 
103:     static string GenerationInterface(SemanticConcept concept, SemanticApplicationContext context) =>
104:         IsGeneratable(concept, context) ? $", global::Cratis.Concepts.IGeneratable<{Identifiers.ToPascalCase(concept.Name)}>" : string.Empty;
105: 
106:     static void RenderFactory(CSharpCodeBuilder builder, SemanticConcept concept, SemanticApplicationContext context)
107:     {
108:         if (!NeedsFactory(concept, context)) return;
109:         var value = IsGeneratable(concept, context) ? "global::Cratis.Concepts.GenerateValue.Uuid()" : new SemanticTypeSystem(context).NotSet(concept.Primitive);
110:         builder.Line($"public static {Identifiers.ToPascalCase(concept.Name)} New() => new({value});").BlankLine();
111:     }
112: 
113:     static void RenderValidator(CSharpCodeBuilder builder, SemanticConcept concept, string name, string rootNamespace)
114:     {
115:         if (concept.Validations.IsEmpty)
116:         {
117:             return;
118:         }
119: 
120:         builder.Using("Cratis.Arc.Validation")
121:             .BlankLine()
122:             .Summary($"Validates {name}.")
123:             .OpenBlock($"public class {name}Validator : global::Cratis.Arc.Validation.ConceptValidator<{name}>")
124:             .OpenBlock($"public {name}Validator()");
125:         foreach (var rule in concept.Validations)
126:         {
127:             SemanticValidationRendering.Render(builder, rule, "Value", concept.Primitive, false, true, rootNamespace: rootNamespace);
128:         }
129: 
130:         builder.EndBlock();
131:         if (concept.Validations.Any(_ => _.Kind == SemanticValidationRuleKind.Matches))
132:         {
133:             SemanticValidationRendering.RenderMatchHelper(builder);
134:         }
135: 
136:         builder.EndBlock();
137:     }
138: }
