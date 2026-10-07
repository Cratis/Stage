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
                .Select(query => (query.Id, Authorization: query.Authorization!, IsCommand: false, Argument: Identifiers.ToCamelCase(query.Argument.Name), Subject: Identifiers.ToCamelCase(query.Argument.Name)))))
            .OrderBy(operation => operation.Id.ToString(), StringComparer.Ordinal).ToArray();

        builder.Summary("Registers every generated authorization policy with Arc.")
            .OpenBlock("public static partial class Registration")
            .OpenBlock("static partial void RegisterGenerated(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        foreach (var operation in operations)
        {
            var anonymous = SemanticCratisAdmission.RequiresAuthentication(operation.Authorization, context.Application.Policies) ? string.Empty : ", evaluatesAnonymous: true";
            builder.Line($"services.AddArcAuthorizationPolicy<{Name(operation.Id)}>({CSharpCodeBuilder.StringLiteral(Name(operation.Id))}{anonymous});");
        }

        builder.EndBlock().EndBlock().BlankLine();
        foreach (var operation in operations)
        {
            var expression = Authorization(operation.Authorization, context.Application.Policies, operation.IsCommand, operation.Argument, operation.Subject);
            builder.Summary("Enforces the effective Screenplay authorization for one operation.")
                .OpenBlock($"public sealed class {Name(operation.Id)} : global::Cratis.Arc.Authorization.IAuthorizationPolicy")
                .Line("/// <inheritdoc/>")
                .ExpressionMember(
                    "public global::System.Threading.Tasks.ValueTask<bool> IsAuthorized(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, global::System.Threading.CancellationToken cancellationToken)",
                    $"global::System.Threading.Tasks.ValueTask.FromResult({expression})")
                .EndBlock().BlankLine();
        }

        // Reflection is deliberately limited to the declared public property path, using ordinal names. A
        // missing value (including a nullable composite or absent query argument) must deny, never match "".
        builder.OpenBlock("internal static class PolicyValues")
            .ExpressionMember(
                "public static bool Match(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, string claim, object? target)",
                "target is not null && context.Principal.Claims.Any(value => global::System.String.Equals(value.Type, claim, global::System.StringComparison.OrdinalIgnoreCase) && (target is global::System.Guid uuid ? global::System.Guid.TryParse(value.Value, out var parsed) && parsed == uuid : target is string text && global::System.String.Equals(value.Value, text, global::System.StringComparison.Ordinal)))")
            .OpenBlock("public static object? Value(object? value)")
            .Line("if (value is string or global::System.Guid) return value;")
            .Line("if (value is null) return null;")
            .Line("var type = value.GetType();")
            .Line("if (!InheritsSupportedConcept(type)) return null;")
            .Line("return type.GetProperty(\"Value\", global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public)?.GetValue(value);")
            .EndBlock()
            .OpenBlock("static bool InheritsSupportedConcept(global::System.Type type)")
            .Line("for (var current = type.BaseType; current is not null; current = current.BaseType)")
            .Line("{")
            .Line("    if (current.IsGenericType && current.GenericTypeArguments.Length == 1 && (current.GenericTypeArguments[0] == typeof(string) || current.GenericTypeArguments[0] == typeof(global::System.Guid)) &&")
            .Line("        (current.GetGenericTypeDefinition().FullName == \"Cratis.Concepts.ConceptAs`1\" || current.GetGenericTypeDefinition().FullName == \"Cratis.Chronicle.Events.EventSourceId`1\")) return true;")
            .Line("}")
            .Line("return false;")
            .EndBlock()
            .OpenBlock("public static object? Path(object? value, string path)")
            .Line("foreach (var segment in path.Split('.'))")
            .Line("{")
            .Line("    if (value is null) return null;")
            .Line("    value = value.GetType().GetProperty(segment, global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public)?.GetValue(value);")
            .Line("}")
            .Line("return Value(value);")
            .EndBlock()
            .OpenBlock("public static object? Query(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, string argument, string path)")
            .Line("if (context.Target is not global::System.Reflection.MethodInfo method || !method.GetParameters().Any(parameter => string.Equals(parameter.Name, argument, global::System.StringComparison.Ordinal)) ||")
            .Line("    context.Resource is not global::Cratis.Arc.Queries.QueryContext { Arguments: { } arguments } || !arguments.TryGetValue(argument, out var key)) return null;")
            .Line("return path == argument ? Value(key) : Path(key, path[(argument.Length + 1)..]);")
            .EndBlock()
            .EndBlock();

        // ESM policies are named rules without SemanticIds; the generated registrations realize protected operations.
        return new(Path.Combine("GeneratedPolicies", "Policies.cs"), builder.ToString())
        {
            Sources = [.. operations.Select(_ => _.Id)]
        };
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
            SemanticClaimTargetKind.Artifact when command => $"PolicyValues.Path((context.Resource as global::Cratis.Arc.Commands.CommandContext)?.Command, {Literal(PascalPath(claim.Value!))})",
            SemanticClaimTargetKind.Artifact => $"PolicyValues.Query(context, {Literal(argument)}, {Literal(QueryPath(claim.Value!))})",
            SemanticClaimTargetKind.Subject when command => $"PolicyValues.Path((context.Resource as global::Cratis.Arc.Commands.CommandContext)?.Command, {Literal(PascalPath(subject))})",
            SemanticClaimTargetKind.Subject => $"PolicyValues.Query(context, {Literal(argument)}, {Literal(argument)})",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticClaimTargetKind), claim.TargetKind)
        };
        return $"PolicyValues.Match(context, {Literal(claim.Claim)}, {target})";
    }

    static string PascalPath(string path) => string.Join('.', path.Split('.').Select(Identifiers.ToPascalCase));

    static string QueryPath(string path) => path.Split('.') is { Length: > 1 } parts
        ? $"{Identifiers.ToCamelCase(parts[0])}.{PascalPath(string.Join('.', parts.Skip(1)))}"
        : Identifiers.ToCamelCase(path);

    static string Literal(string value) => JsonSerializer.Serialize(value);
}
