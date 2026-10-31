// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Validation;

/// <summary>
/// Renders dependency-free validation contexts and their verified bodies.
/// </summary>
internal static class SemanticValidationContextRuntime
{
    internal static readonly string[] RefusedMembers = ["Tenant", "CausedBy"];

    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SemanticApplicationContext, Dictionary<string, IReadOnlySet<string>>> _contextReads = [];

    internal static bool IsBody(SemanticValidationRule rule) => rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation;

    internal static bool IsIterator(SemanticTypedContextDescriptor descriptor) => descriptor.Role is SemanticImplementationRole.CommandValidation or SemanticImplementationRole.ConceptValidation;

    internal static IEnumerable<SemanticTypedContextDescriptor> Selected(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var owners = slices.SelectMany(slice => slice.Slice.Commands).Select(command => command.Id)
            .Concat(context.Application.Concepts.Select(concept => concept.Id)).ToHashSet();
        return context.Request.TypedContextDescriptors.Where(descriptor => (descriptor.Role == SemanticImplementationRole.RulePredicate || IsIterator(descriptor)) &&
            descriptor.OperationId is { } owner && owners.Contains(owner));
    }

    internal static string Wrapper(SemanticTypedContextDescriptor descriptor) => $"TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}";

    internal static string Method(SemanticTypedContextDescriptor descriptor) => $"{(IsIterator(descriptor) ? "Validate" : "Rule")}_{SemanticTypedContextRenderer.Suffix(descriptor)}";

    internal static RenderedFile RenderWrapper(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, bool analysis = false)
    {
        var supplied = descriptor with
        {
            Members = [.. descriptor.Members.Where(member => !RefusedMembers.Contains(member.Name, StringComparer.Ordinal) &&
                (member.Name != "Occurred" || analysis || ReadsOccurred(context, descriptor)))
                .Select(member => PrimitiveConceptMember(context, member))]
        };
        RenderedFile file;
        using (analysis ? QualifiedSpelling.Begin() : null)
        {
            file = analysis ? AnalysisWrapper(context, supplied) : SemanticTypedContextRenderer.Render(supplied, context);
        }

        var accessor = Accessors(context, supplied, analysis);
        if (accessor.Length > 0)
        {
            var closing = file.Content.LastIndexOf('}');
            file = file with { Content = file.Content.Insert(closing, accessor) };
        }
        if (!analysis) return file;
        var marker = $"public record {Wrapper(descriptor)}(";
        return file with { Content = file.Content.Replace(marker, marker + "string Tenant, string CausedBy, ", StringComparison.Ordinal) };
    }

    internal static SemanticTypedContextMember? UnsupportedAnalysisMember(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor) =>
        descriptor.Members
            .Where(member => !member.IsDerived && !RefusedMembers.Contains(member.Name, StringComparer.Ordinal))
            .Select(member => PrimitiveConceptMember(context, member))
            .FirstOrDefault(member => member.Type.ModelType is null &&
            member.Type.Shape is null &&
            !IsAnalysisRuntimeToken(member.Type.RuntimeToken));

    internal static RenderedFile AnalysisWrapper(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor)
    {
        var types = new SemanticTypeSystem(context);
        var parameters = descriptor.Members.Where(member => !member.IsDerived).Select(member => AnalysisParameter(context, types, member));
        var builder = new CSharpCodeBuilder().Namespace(context.TypedContextsNamespace)
            .OpenBlock($"public record {Wrapper(descriptor)}({string.Join(", ", parameters)})");
        builder.Line("public bool IsWholeArtifact => global::System.String.IsNullOrEmpty(Property);").EndBlock();
        return new("ValidationContext.cs", builder.ToString());
    }

    internal static RenderedFile RenderBodies(SemanticApplicationContext context, IEnumerable<(SemanticTypedContextDescriptor Descriptor, string Body)> bodies, bool analysis = false)
    {
        var builder = (analysis ? new CSharpCodeBuilder() : context.CodeBuilder()).Namespace(context.TypedContextsNamespace);
        foreach (var owner in bodies.Select(pair => pair.Descriptor.OperationId).Distinct())
        {
            if (owner is { } id && context.Commands.ContainsKey(id))
                builder.Using(SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(id).Path));
        }
        builder.OpenBlock("internal static class ValidationBodies");
        foreach (var body in bodies) RenderBody(builder, context, body, analysis);
        builder.EndBlock();
        return new(Path.Combine(context.TypedContextsFolder, "ValidationBodies.cs"), builder.ToString());
    }

    internal static string AnalysisDeclarations(SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var declarations = context.Application.Concepts.Select(concept => concept.Values.IsEmpty
            ? $"public record {Identifiers.ToPascalCase(concept.Name)}({types.Primitive(concept.Primitive)} Value) {{ public static implicit operator {types.Primitive(concept.Primitive)}({Identifiers.ToPascalCase(concept.Name)} value) => value.Value; }}"
            : $"public enum {Identifiers.ToPascalCase(concept.Name)} {{ {string.Join(", ", SemanticEnumMembers.Declarations(concept))} }}")
            .Concat(SemanticBodyDeclarations.CompositeTypes(context));
        var code = $"namespace {context.CommonNamespace} {{ {string.Join('\n', declarations)} }}\n";
        return code + SemanticBodyDeclarations.Commands(context);
    }

    internal static void RenderIterator(CSharpCodeBuilder builder, SemanticApplicationContext context, string requirementId, SemanticId owner, SemanticProperty? property = null)
    {
        var descriptor = context.Request.TypedContextDescriptors.Single(descriptor => descriptor.RequirementId == requirementId && descriptor.OperationId == owner);
        var ruleFor = property is null && context.Concepts.ContainsKey(owner)
            ? $"RuleFor<{new SemanticTypeSystem(context).Type(SemanticTypeReference.ForConcept(owner))}>" : "RuleFor";
        var selector = property is null ? "_" : $"_.{context.MemberNames.Property(property)}";
        var path = CSharpCodeBuilder.StringLiteral(PropertyPath(context, property));
        var artifact = ConceptValue(context, owner, "validation.InstanceToValidate");
        var value = ConceptValue(context, owner, "value");
        builder.OpenBlock($"{ruleFor}(_ => {selector}).Custom((value, validation) =>")
            .OpenBlock($"foreach (var message in global::{context.TypedContextsNamespace}.ValidationBodies.{Method(descriptor)}(new global::{context.TypedContextsNamespace}.{Wrapper(descriptor)}({artifact}, {value}, {path}{OccurredArgument(context, descriptor)})))")
            .Line("validation.AddFailure(message);")
            .EndBlock().EndBlock().Line(");");
    }

    internal static void RenderRule(CSharpCodeBuilder builder, SemanticApplicationContext context, SemanticValidationRule rule, SemanticId owner, SemanticProperty? property)
    {
        if (rule.Kind == SemanticValidationRuleKind.CodeValidation)
        {
            RenderIterator(builder, context, rule.RequirementId!, owner, property);
            return;
        }
        var descriptor = context.Request.TypedContextDescriptors.Single(descriptor => descriptor.RequirementId == rule.RequirementId && descriptor.OperationId == owner);
        var type = property is null ? new SemanticTypeSystem(context).Type(SemanticTypeReference.ForConcept(owner)) : new SemanticTypeSystem(context).Type(property.Type);
        var selector = property is null ? "_" : $"_.{context.MemberNames.Property(property)}";
        var artifact = property is null ? ConceptValue(context, owner, "value") : "artifact";
        var value = ConceptValue(context, owner, "value");
        var path = CSharpCodeBuilder.StringLiteral(PropertyPath(context, property));
        var call = $"global::{context.TypedContextsNamespace}.ValidationBodies.{Method(descriptor)}(new global::{context.TypedContextsNamespace}.{Wrapper(descriptor)}({artifact}, {value}, {path}{OccurredArgument(context, descriptor)}))";
        var message = rule.Message ?? $"A value does not satisfy the '{rule.Kind}' rule.";
        var localized = message.StartsWith("$strings.", StringComparison.Ordinal);
        var renderedMessage = localized ? $"_ => global::{context.RootNamespace}.GeneratedStrings.Resolve({CSharpCodeBuilder.StringLiteral(message)})" : CSharpCodeBuilder.StringLiteral(message);
        var state = localized ? $".WithState({CSharpCodeBuilder.StringLiteral(message)})" : string.Empty;
        var severity = rule.Severity == SemanticValidationSeverity.Error ? string.Empty : $".WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.{rule.Severity})";
        var predicate = property is null ? $"value => {call}" : $"(artifact, value) => {call}";
        builder.Line($"RuleFor<{type}>(_ => {selector}).Must({predicate}).WithMessage({renderedMessage}){state}{severity};");
    }

    internal static bool UsesCommandReceiptTime(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices) =>
        GeneratedTypeNames.UsesCommandReceiptTime(slices.SelectMany(located => located.Slice.Commands)) ||
        Selected(context, slices).Any(descriptor => ReadsOccurred(context, descriptor));

    internal static bool CommandReadsOccurred(SemanticApplicationContext context, SemanticCommand command) =>
        SemanticOpaqueBodyDependencies.ForCommand(context, command).Any(dependency => ReadsOccurred(context, dependency.Descriptor));

    internal static bool ReadsOccurred(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor) => ContextReads(context, descriptor).Contains("Occurred");

    internal static IReadOnlySet<string> ContextReads(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor)
    {
        var cache = _contextReads.GetOrCreateValue(context);
        var key = $"{descriptor.RequirementId}:{descriptor.OperationId}";
        if (cache.TryGetValue(key, out var reads)) return reads;
        var diagnostics = System.Collections.Immutable.ImmutableArray.CreateBuilder<Contracts.Rendering.ArtifactRenderDiagnostic>();
        if (!SemanticImplementationAdmission.TryGetVerifiedBody(context.Request, descriptor.RequirementId, descriptor.OperationId!.Value, diagnostics, out var body)) return System.Collections.Immutable.ImmutableHashSet<string>.Empty;
        var requirement = context.Request.ImplementationRequirements.Single(requirement => requirement.RequirementId == descriptor.RequirementId);
        reads = PureValidationAdmission.AnalyzeReads(body!, context, descriptor, requirement).ContextReads;
        cache.Add(key, reads);
        return reads;
    }

    static string OccurredArgument(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor) => ReadsOccurred(context, descriptor)
        ? $", global::{context.RootNamespace}.GeneratedCommands.CommandReceiptTime.OccurredAtReceipt(new global::Cratis.Arc.OperationContextAccessor())" : string.Empty;

    static string AnalysisParameter(SemanticApplicationContext context, SemanticTypeSystem types, SemanticTypedContextMember member)
    {
        var type = AnalysisMemberType(context, types, member);
        if (member.IsNullable && !type.EndsWith('?')) type += "?";
        return $"{type} {member.Name}";
    }

    static string AnalysisMemberType(SemanticApplicationContext context, SemanticTypeSystem types, SemanticTypedContextMember member)
    {
        if (member.Type.ModelType is { } reference) return types.Type(reference, reference.IsCollection);
        if (member.Type.Shape is { } binding) return AnalysisShapeType(context, binding);
        return member.Type.RuntimeToken switch
        {
            SemanticContextRuntimeTokens.Text => "string",
            SemanticContextRuntimeTokens.DateTime => "global::System.DateTimeOffset",
            SemanticContextRuntimeTokens.Boolean => "bool",
            _ => throw new InvalidOperationException($"Unsupported validation context member '{member.Name}'.")
        };
    }

    static bool IsAnalysisRuntimeToken(string? token) =>
        token == SemanticContextRuntimeTokens.Text ||
        token == SemanticContextRuntimeTokens.DateTime ||
        token == SemanticContextRuntimeTokens.Boolean;

    static string AnalysisShapeType(SemanticApplicationContext context, SemanticId binding)
    {
        var command = context.Commands[binding];
        return $"global::{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(binding).Path)}.{Identifiers.ToPascalCase(command.Name)}";
    }

    static string Accessors(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, bool analysis)
    {
        var reads = analysis ? new HashSet<string>(["ValueAs", "ArtifactAs"]) : ContextReads(context, descriptor);
        const string artifactMethod = "    public T ArtifactAs<T>() => Artifact is T artifact ? artifact : throw new global::System.InvalidCastException();\n";
        return (reads.Contains("ValueAs") ? ValueAccessor(context, descriptor) : string.Empty) +
            (reads.Contains("ArtifactAs") ? artifactMethod : string.Empty);
    }

    static string ValueAccessor(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor)
    {
        var (reference, expression) = ValueSource(context, descriptor);
        var conversion = WholeNumberConversion(reference, expression);
        if (expression != "Value") conversion += $"if ({expression} is T primitive) return primitive;\n";
        return $$"""
                public T ValueAs<T>()
                {
                    if (Value is T value) return value;
                    if ((object?)Value is null)
                    {
                        if (default(T) is null) return default!;
                        throw new global::System.InvalidCastException();
                    }
                    {{conversion}}
                    throw new global::System.InvalidCastException();
                }
            """;
    }

    static (SemanticTypeReference? Type, string Expression) ValueSource(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor)
    {
        var reference = descriptor.Members.Single(member => member.Name == "Value").Type.ModelType;
        if (reference?.Kind != SemanticTypeReferenceKind.Concept) return (reference, "Value");
        if (!context.Concepts.TryGetValue(reference.Target, out var concept)) return (reference, "Value");
        return concept.Values.IsEmpty ? (SemanticTypeReference.ForPrimitive(concept.Primitive), "Value.Value") : (reference, "Value");
    }

    static string WholeNumberConversion(SemanticTypeReference? reference, string expression)
    {
        if (reference?.Primitive != SemanticPrimitiveType.WholeNumber) return string.Empty;
        return reference.IsCollection
            ? "if (typeof(T) == typeof(global::System.Collections.Generic.IEnumerable<int>)) return (T)(object)global::System.Linq.Enumerable.Select(Value, static value => checked((int)value));\nif (typeof(T) == typeof(global::System.Collections.Generic.IEnumerable<long>)) return (T)(object)global::System.Linq.Enumerable.Select(Value, static value => checked((long)value));\n"
            : $"if (typeof(T) == typeof(int) || typeof(T) == typeof(int?)) return (T)(object)checked((int){expression});\nif (typeof(T) == typeof(long) || typeof(T) == typeof(long?)) return (T)(object)checked((long){expression});\n";
    }

    static SemanticTypedContextMember PrimitiveConceptMember(SemanticApplicationContext context, SemanticTypedContextMember member)
    {
        if (member.Source.Kind != SemanticContextSourceKinds.ConceptValue) return member;
        if (member.Source.SemanticId is not { } id) return member;
        if (!context.Concepts.TryGetValue(id, out var concept)) return member;
        if (!concept.Values.IsEmpty) return member;
        return member with { Type = member.Type with { ModelType = SemanticTypeReference.ForPrimitive(concept.Primitive) } };
    }

    static void RenderBody(CSharpCodeBuilder builder, SemanticApplicationContext context, (SemanticTypedContextDescriptor Descriptor, string Body) body, bool analysis)
    {
        var (descriptor, code) = body;
        var iterator = IsIterator(descriptor);
        var result = iterator ? "global::System.Collections.Generic.IEnumerable<string>" : "bool";
        builder.OpenBlock($"public static {result} {Method(descriptor)}(global::{context.TypedContextsNamespace}.{Wrapper(descriptor)} context)");
        if (analysis)
        {
            builder.RawVerbatim(code).EndBlock().BlankLine();
            return;
        }
        var implementation = iterator ? $"return global::System.Linq.Enumerable.ToArray({Method(descriptor)}_Core(context));" : code;
        RenderBoundary(builder, implementation, iterator, iterator ? null : descriptor.RequirementId);
        builder.EndBlock().BlankLine();
        if (!iterator) return;
        builder.OpenBlock($"static {result} {Method(descriptor)}_Core(global::{context.TypedContextsNamespace}.{Wrapper(descriptor)} context)")
            .OpenBlock("checked").RawVerbatim(InvariantValidationInterpolation.Render(code), descriptor.RequirementId).EndBlock().EndBlock().BlankLine();
    }

    static void RenderBoundary(CSharpCodeBuilder builder, string body, bool iterator, string? requirementId)
    {
        builder.Line("var culture = global::System.Globalization.CultureInfo.CurrentCulture;")
            .Line("var uiCulture = global::System.Globalization.CultureInfo.CurrentUICulture;")
            .OpenBlock("try")
            .Line("global::System.Globalization.CultureInfo.CurrentCulture = global::System.Globalization.CultureInfo.InvariantCulture;")
            .Line("global::System.Globalization.CultureInfo.CurrentUICulture = global::System.Globalization.CultureInfo.InvariantCulture;")
            .OpenBlock("checked").RawVerbatim(body, requirementId).EndBlock().EndBlock()
            .OpenBlock("catch (global::System.Exception)")
            .Line(iterator ? "return [\"Validation body failed.\"];" : "return false;").EndBlock()
            .OpenBlock("finally")
            .Line("global::System.Globalization.CultureInfo.CurrentCulture = culture;")
            .Line("global::System.Globalization.CultureInfo.CurrentUICulture = uiCulture;").EndBlock();
    }

    static string ConceptValue(SemanticApplicationContext context, SemanticId owner, string expression) =>
        context.Concepts.TryGetValue(owner, out var concept) && concept.Values.IsEmpty ? $"{expression}.Value" : expression;

    static string PropertyPath(SemanticApplicationContext context, SemanticProperty? property)
    {
        if (property is null) return string.Empty;
        return context.MemberNames.WireProperty(property);
    }
}
