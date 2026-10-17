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

    internal static bool IsBody(SemanticValidationRule rule) => rule.Kind == SemanticValidationRuleKind.RulePredicate;

    internal static IEnumerable<SemanticTypedContextDescriptor> Selected(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var owners = slices.SelectMany(slice => slice.Slice.Commands).Select(command => command.Id)
            .Concat(context.Application.Concepts.Select(concept => concept.Id)).ToHashSet();
        return context.Request.TypedContextDescriptors.Where(descriptor => descriptor.Role == SemanticImplementationRole.RulePredicate &&
            descriptor.OperationId is { } owner && owners.Contains(owner));
    }

    internal static string Wrapper(SemanticTypedContextDescriptor descriptor) => $"TypedContext_{SemanticTypedContextRenderer.Suffix(descriptor)}";

    internal static string Method(SemanticTypedContextDescriptor descriptor) => $"Rule_{SemanticTypedContextRenderer.Suffix(descriptor)}";

    internal static RenderedFile RenderWrapper(SemanticApplicationContext context, SemanticTypedContextDescriptor descriptor, bool analysis = false)
    {
        var supplied = descriptor with { Members = [.. descriptor.Members.Where(member => !RefusedMembers.Contains(member.Name, StringComparer.Ordinal))] };
        var file = SemanticTypedContextRenderer.Render(supplied, context);
        if (!analysis) return file;
        var marker = $"public record {Wrapper(descriptor)}(";
        return file with { Content = file.Content.Replace(marker, marker + "string Tenant, string CausedBy, global::System.DateTimeOffset Occurred, ", StringComparison.Ordinal) };
    }

    internal static RenderedFile RenderBodies(SemanticApplicationContext context, IEnumerable<(SemanticTypedContextDescriptor Descriptor, string Body)> bodies)
    {
        var builder = new CSharpCodeBuilder().Namespace(context.TypedContextsNamespace)
            .OpenBlock("internal static class ValidationBodies");
        foreach (var (descriptor, body) in bodies)
        {
            builder.OpenBlock($"public static bool {Method(descriptor)}(global::{context.TypedContextsNamespace}.{Wrapper(descriptor)} context)")
                .RawVerbatim(body).EndBlock().BlankLine();
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
            .Concat(context.Application.Types.Select(type => $"public record {Identifiers.ToPascalCase(type.Name)}({Parameters(type.Properties)});"));
        var code = $"namespace {context.CommonNamespace} {{ {string.Join('\n', declarations)} }}\n";
        foreach (var command in context.Commands.Values)
        {
            code += $"namespace {SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(command.Id).Path)} {{ public record {Identifiers.ToPascalCase(command.Name)}({Parameters(command.Properties.Where(property => !property.IsGenerated))}); }}\n";
        }
        return code;

        string Parameters(IEnumerable<SemanticProperty> properties) => string.Join(", ", properties.Select(property => $"{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}"));
    }

    internal static void RenderRule(CSharpCodeBuilder builder, SemanticApplicationContext context, SemanticValidationRule rule, SemanticId owner, SemanticProperty? property)
    {
        var descriptor = context.Request.TypedContextDescriptors.Single(descriptor => descriptor.RequirementId == rule.RequirementId && descriptor.OperationId == owner);
        var type = property is null ? new SemanticTypeSystem(context).Type(SemanticTypeReference.ForConcept(owner)) : new SemanticTypeSystem(context).Type(property.Type);
        var selector = property is null ? "_" : $"_.{Identifiers.ToPascalCase(property.Name)}";
        var artifact = property is null ? "value" : "artifact";
        var path = CSharpCodeBuilder.StringLiteral(property?.Name ?? string.Empty);
        var call = $"global::{context.TypedContextsNamespace}.ValidationBodies.{Method(descriptor)}(new global::{context.TypedContextsNamespace}.{Wrapper(descriptor)}({artifact}, value, {path}))";
        var message = rule.Message ?? $"A value does not satisfy the '{rule.Kind}' rule.";
        var localized = message.StartsWith("$strings.", StringComparison.Ordinal);
        var renderedMessage = localized ? $"_ => global::{context.RootNamespace}.GeneratedStrings.Resolve({CSharpCodeBuilder.StringLiteral(message)})" : CSharpCodeBuilder.StringLiteral(message);
        var state = localized ? $".WithState({CSharpCodeBuilder.StringLiteral(message)})" : string.Empty;
        var severity = rule.Severity == SemanticValidationSeverity.Error ? string.Empty : $".WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.{rule.Severity})";
        var predicate = property is null ? $"value => {call}" : $"(artifact, value) => {call}";
        builder.Line($"RuleFor<{type}>(_ => {selector}).Must({predicate}).WithMessage({renderedMessage}){state}{severity};");
    }
}
