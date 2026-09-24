// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies;

/// <summary>
/// Renders one scoped Arc policy for each protected operation and its exact effective authorization expression.
/// </summary>
internal static class SemanticPolicyArtifactRenderer
{
    public static string Name(SemanticId id) => $"StagePolicy_{id.ToString().Replace('-', '_').Replace(':', '_')}";

    public static RenderedFile Render(SemanticApplicationContext context, IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var builder = new CSharpCodeBuilder()
            .Namespace($"{context.RootNamespace}.GeneratedPolicies")
            .Using("System.Reflection")
            .Using("Cratis.Arc.Authorization")
            .Using("Cratis.Arc.Commands")
            .Using("Cratis.Arc.Queries")
            .Using("Microsoft.Extensions.DependencyInjection");
        var operations = slices.SelectMany(slice => slice.Slice.Commands.Where(command => command.Authorization is not null)
                .Select(command => (command.Id, Authorization: command.Authorization!, IsCommand: true, Argument: string.Empty, Subject: command.Properties.Single(property => property.IsIdentifier).Name)))
            .Concat(slices.SelectMany(slice => slice.Slice.Queries.Where(query => query.Authorization is not null)
                .Select(query => (query.Id, Authorization: query.Authorization!, IsCommand: false, Argument: query.Argument.Name, Subject: query.Argument.Name))))
            .OrderBy(operation => operation.Id.ToString(), StringComparer.Ordinal).ToArray();

        builder.Summary("Registers every generated authorization policy with Arc.")
            .OpenBlock("public static partial class Registration")
            .OpenBlock("static partial void RegisterGenerated(IServiceCollection services)");
        foreach (var operation in operations)
        {
            builder.Line($"services.AddArcAuthorizationPolicy<{Name(operation.Id)}>({CSharpCodeBuilder.StringLiteral(Name(operation.Id))});");
        }

        builder.EndBlock().EndBlock().BlankLine();
        foreach (var operation in operations)
        {
            var expression = Authorization(operation.Authorization, context.Application.Policies, operation.IsCommand, operation.Argument, operation.Subject);
            builder.Summary("Enforces the effective Screenplay authorization for one operation.")
                .OpenBlock($"public sealed class {Name(operation.Id)} : IAuthorizationPolicy")
                .Line("/// <inheritdoc/>")
                .ExpressionMember(
                    "public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)",
                    $"ValueTask.FromResult({expression})")
                .EndBlock().BlankLine();
        }

        // Reflection is deliberately limited to the declared public property path, using ordinal names. A
        // missing value (including a nullable composite or absent query argument) must deny, never match "".
        builder.OpenBlock("internal static class PolicyValues")
            .ExpressionMember(
                "public static bool Match(AuthorizationPolicyContext context, string claim, string? target)",
                "target is not null && context.Principal.Claims.Any(value => string.Equals(value.Type, claim, StringComparison.OrdinalIgnoreCase) && string.Equals(value.Value, target, StringComparison.Ordinal))")
            .OpenBlock("public static string? Text(object? value)")
            .Line("if (value is string text) return text;")
            .Line("if (value is null) return null;")
            .Line("var type = value.GetType();")
            .Line("if (!InheritsTextConcept(type)) return null;")
            .Line("return type.GetProperty(\"Value\", BindingFlags.Instance | BindingFlags.Public)?.GetValue(value) as string;")
            .EndBlock()
            .OpenBlock("static bool InheritsTextConcept(Type type)")
            .Line("for (var current = type.BaseType; current is not null; current = current.BaseType)")
            .Line("{")
            .Line("    if (current.IsGenericType && current.GenericTypeArguments.Length == 1 && current.GenericTypeArguments[0] == typeof(string) &&")
            .Line("        (current.GetGenericTypeDefinition().FullName == \"Cratis.Concepts.ConceptAs`1\" || current.GetGenericTypeDefinition().FullName == \"Cratis.Chronicle.Events.EventSourceId`1\")) return true;")
            .Line("}")
            .Line("return false;")
            .EndBlock()
            .OpenBlock("public static string? Path(object? value, string path)")
            .Line("foreach (var segment in path.Split('.'))")
            .Line("{")
            .Line("    if (value is null) return null;")
            .Line("    value = value.GetType().GetProperty(segment, BindingFlags.Instance | BindingFlags.Public)?.GetValue(value);")
            .Line("}")
            .Line("return Text(value);")
            .EndBlock()
            .OpenBlock("public static string? Query(AuthorizationPolicyContext context, string argument, string path)")
            .Line("if (context.Resource is not QueryContext { Arguments: { } arguments } || !arguments.TryGetValue(argument, out var key)) return null;")
            .Line("return path == argument ? Text(key) : Path(key, path[(argument.Length + 1)..]);")
            .EndBlock()
            .EndBlock();
        return new(Path.Combine("GeneratedPolicies", "Policies.cs"), builder.ToString());
    }

    static string Authorization(SemanticAuthorization authorization, IEnumerable<SemanticPolicy> policies, bool command, string argument, string subject) => authorization switch
    {
        SemanticPolicyReference reference => Condition(policies.Single(policy => policy.Name == reference.Name).Condition, command, argument, subject),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical => $"({Authorization(logical.Left, policies, command, argument, subject)} && {Authorization(logical.Right, policies, command, argument, subject)})",
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical => $"({Authorization(logical.Left, policies, command, argument, subject)} || {Authorization(logical.Right, policies, command, argument, subject)})",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticAuthorization), authorization.GetType().Name)
    };

    static string Condition(SemanticPolicyCondition condition, bool command, string argument, string subject) => condition switch
    {
        SemanticAuthenticatedCondition => "context.Principal.Identity?.IsAuthenticated == true",
        SemanticRoleCondition role => $"context.Principal.IsInRole({Literal(role.Role)})",
        SemanticClaimCondition claim => Claim(claim, command, argument, subject),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical => $"({Condition(logical.Left, command, argument, subject)} && {Condition(logical.Right, command, argument, subject)})",
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical => $"({Condition(logical.Left, command, argument, subject)} || {Condition(logical.Right, command, argument, subject)})",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPolicyCondition), condition.GetType().Name)
    };

    static string Claim(SemanticClaimCondition claim, bool command, string argument, string subject)
    {
        var target = claim.TargetKind switch
        {
            SemanticClaimTargetKind.Literal => Literal(claim.Value!),
            SemanticClaimTargetKind.Artifact when command => $"PolicyValues.Path((context.Resource as CommandContext)?.Command, {Literal(PascalPath(claim.Value!))})",
            SemanticClaimTargetKind.Artifact => $"PolicyValues.Query(context, {Literal(argument)}, {Literal(QueryPath(claim.Value!))})",
            SemanticClaimTargetKind.Subject when command => $"PolicyValues.Path((context.Resource as CommandContext)?.Command, {Literal(PascalPath(subject))})",
            SemanticClaimTargetKind.Subject => $"PolicyValues.Query(context, {Literal(argument)}, {Literal(argument)})",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticClaimTargetKind), claim.TargetKind)
        };
        return $"PolicyValues.Match(context, {Literal(claim.Claim)}, {target})";
    }

    static string PascalPath(string path) => string.Join('.', path.Split('.').Select(Identifiers.ToPascalCase));

    static string QueryPath(string path) => path.Split('.') is { Length: > 1 } parts
        ? $"{parts[0]}.{PascalPath(string.Join('.', parts.Skip(1)))}"
        : path;

    static string Literal(string value) => JsonSerializer.Serialize(value);
}
