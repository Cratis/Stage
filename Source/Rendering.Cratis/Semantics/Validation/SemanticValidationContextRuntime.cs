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
    internal static readonly string[] RefusedMembers = ["Tenant", "CausedBy", "Occurred"];

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
            Members = [.. descriptor.Members.Where(member => !RefusedMembers.Contains(member.Name, StringComparer.Ordinal))
                .Select(member => PrimitiveConceptMember(context, member))]
        };
        RenderedFile file;
        using (analysis ? QualifiedSpelling.Begin() : null)
        {
            file = SemanticTypedContextRenderer.Render(supplied, context);
        }

        if (!analysis) return file;
        var marker = $"public record {Wrapper(descriptor)}(";
        return file with { Content = file.Content.Replace(marker, marker + "string Tenant, string CausedBy, global::System.DateTimeOffset Occurred, ", StringComparison.Ordinal) };
    }

    internal static RenderedFile RenderBodies(SemanticApplicationContext context, IEnumerable<(SemanticTypedContextDescriptor Descriptor, string Body)> bodies, bool analysis = false)
    {
        var builder = (analysis ? new CSharpCodeBuilder() : context.CodeBuilder()).Namespace(context.TypedContextsNamespace)
            .OpenBlock("internal static class ValidationBodies");
        foreach (var (descriptor, body) in bodies)
        {
            var iterator = IsIterator(descriptor);
            var result = iterator ? "global::System.Collections.Generic.IEnumerable<string>" : "bool";
            builder.OpenBlock($"public static {result} {Method(descriptor)}(global::{context.TypedContextsNamespace}.{Wrapper(descriptor)} context)")
                .RawVerbatim(iterator && !analysis ? InvariantValidationInterpolation.Render(body) : body).EndBlock().BlankLine();
        }
        builder.EndBlock();
        return new(Path.Combine(context.TypedContextsFolder, "ValidationBodies.cs"), builder.ToString());
    }

    internal static string AnalysisDeclarations(SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var declarations = context.Application.Concepts.Select(concept => concept.Values.IsEmpty
            ? $"public record {Identifiers.ToPascalCase(concept.Name)}({types.Primitive(concept.Primitive)} Value);"
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
            .OpenBlock($"foreach (var message in global::{context.TypedContextsNamespace}.ValidationBodies.{Method(descriptor)}(new global::{context.TypedContextsNamespace}.{Wrapper(descriptor)}({artifact}, {value}, {path})))")
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
        var call = $"global::{context.TypedContextsNamespace}.ValidationBodies.{Method(descriptor)}(new global::{context.TypedContextsNamespace}.{Wrapper(descriptor)}({artifact}, {value}, {path}))";
        var message = rule.Message ?? $"A value does not satisfy the '{rule.Kind}' rule.";
        var localized = message.StartsWith("$strings.", StringComparison.Ordinal);
        var renderedMessage = localized ? $"_ => global::{context.RootNamespace}.GeneratedStrings.Resolve({CSharpCodeBuilder.StringLiteral(message)})" : CSharpCodeBuilder.StringLiteral(message);
        var state = localized ? $".WithState({CSharpCodeBuilder.StringLiteral(message)})" : string.Empty;
        var severity = rule.Severity == SemanticValidationSeverity.Error ? string.Empty : $".WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.{rule.Severity})";
        var predicate = property is null ? $"value => {call}" : $"(artifact, value) => {call}";
        builder.Line($"RuleFor<{type}>(_ => {selector}).Must({predicate}).WithMessage({renderedMessage}){state}{severity};");
    }

    static SemanticTypedContextMember PrimitiveConceptMember(SemanticApplicationContext context, SemanticTypedContextMember member)
    {
        if (member.Source.Kind != SemanticContextSourceKinds.ConceptValue) return member;
        if (member.Source.SemanticId is not { } id) return member;
        if (!context.Concepts.TryGetValue(id, out var concept)) return member;
        if (!concept.Values.IsEmpty) return member;
        return member with { Type = member.Type with { ModelType = SemanticTypeReference.ForPrimitive(concept.Primitive) } };
    }

    static string ConceptValue(SemanticApplicationContext context, SemanticId owner, string expression) =>
        context.Concepts.TryGetValue(owner, out var concept) && concept.Values.IsEmpty ? $"{expression}.Value" : expression;

    static string PropertyPath(SemanticApplicationContext context, SemanticProperty? property)
    {
        if (property is null) return string.Empty;
        return context.MemberNames.WireProperty(property);
    }
}
