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
                .Select(command => (command.Id, Authorization: command.Authorization!, IsCommand: true, Argument: string.Empty, Subject: command.Properties.Single(property => property.IsIdentifier).Name, Properties: (IReadOnlyList<SemanticProperty>)command.Properties)))
            .Concat(slices.SelectMany(slice => slice.Slice.Queries.Where(query => query.Authorization is not null)
                .Select(query => (query.Id, Authorization: query.Authorization!, IsCommand: false, Argument: Identifiers.ToCamelCase(query.Argument.Name), Subject: query.Argument.Name, Properties: (IReadOnlyList<SemanticProperty>)[new(query.Argument.Id, query.Argument.Name, query.Argument.Type, false)]))))
            .OrderBy(operation => operation.Id.ToString(), StringComparer.Ordinal).ToArray();
        var negates = operations.Any(operation => Negates(operation.Authorization, context.Application.Policies));

        builder.Summary("Registers every generated authorization policy with Arc.")
            .OpenBlock("public static partial class Registration")
            .OpenBlock("static partial void RegisterGenerated(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        foreach (var operation in operations)
        {
            // Arc strips guest roles and claims. Opt in only when this operation's effective authorization
            // can allow that empty principal; the policy still checks the actual request, including unknown targets.
            var anonymous = AllowsGuest(operation.Authorization, context, operation.Properties, operation.Subject) ? ", evaluatesAnonymous: true" : string.Empty;
            builder.Line($"services.AddArcAuthorizationPolicy<{Name(operation.Id)}>({CSharpCodeBuilder.StringLiteral(Name(operation.Id))}{anonymous});");
        }

        builder.EndBlock().EndBlock().BlankLine();
        foreach (var operation in operations)
        {
            var expression = Authorization(operation.Authorization, context, operation.Properties, operation.IsCommand, operation.Argument, operation.Subject);
            builder.Summary("Enforces the effective Screenplay authorization for one operation.")
                .OpenBlock($"public sealed class {Name(operation.Id)} : global::Cratis.Arc.Authorization.IAuthorizationPolicy")
                .Line("/// <inheritdoc/>")
                .ExpressionMember(
                    "public global::System.Threading.Tasks.ValueTask<bool> IsAuthorized(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, global::System.Threading.CancellationToken cancellationToken)",
                    $"global::System.Threading.Tasks.ValueTask.FromResult({expression})")
                .EndBlock().BlankLine();
        }

        // Reflection is limited to the declared public property path, using ordinal names. Missing values
        // deny; an empty-string text target matches an empty claim. Uuid targets must be canonical.
        builder.OpenBlock("internal static class PolicyValues")
            .ExpressionMember(
                "public static bool Match(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, string claim, string? target)",
                "target is not null && context.Principal.Claims.Any(value => global::System.String.Equals(value.Type, claim, global::System.StringComparison.OrdinalIgnoreCase) && global::System.String.Equals(value.Value, target, global::System.StringComparison.Ordinal))");
        if (negates)
        {
            // Screenplay's three-valued policy logic, with null as unknown: a comparison whose target is missing, null
            // or not text is unknown, negation keeps it unknown, and/or decide on a definite operand in either order,
            // and only a definite true allows.
            builder.ExpressionMember(
                    "public static bool? Truth(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, string claim, string? target)",
                    "target is null ? null : Match(context, claim, target)")
                .ExpressionMember("public static bool? Not(bool? value)", "value is null ? null : !value.Value")
                .ExpressionMember("public static bool? And(bool? left, bool? right)", "(left, right) switch { (false, _) or (_, false) => false, (true, true) => true, _ => null }")
                .ExpressionMember("public static bool? Or(bool? left, bool? right)", "(left, right) switch { (true, _) or (_, true) => true, (false, false) => false, _ => null }");
        }

        builder
            .OpenBlock("public static string? Uuid(object? value)")
            .Line("if (value is global::System.Guid uuid) return uuid.ToString(\"D\");")
            .Line("if (value is string text && global::System.Guid.TryParseExact(text, \"D\", out var parsed) &&")
            .Line("    global::System.String.Equals(text, parsed.ToString(\"D\"), global::System.StringComparison.Ordinal)) return text;")
            .Line("return null;")
            .EndBlock()
            .OpenBlock("public static string? Value(object? value, bool uuidTarget = false)")
            .Line("if (value is string text) return uuidTarget ? Uuid(text) : text;")
            .Line("if (value is global::System.Guid uuid) return Uuid(uuid);")
            .Line("if (value is null) return null;")
            .Line("var type = value.GetType();")
            .Line("if (!InheritsSupportedConcept(type)) return null;")
            .Line("return Value(type.GetProperty(\"Value\", global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public)?.GetValue(value), uuidTarget);")
            .EndBlock()
            .OpenBlock("static bool InheritsSupportedConcept(global::System.Type type)")
            .Line("for (var current = type.BaseType; current is not null; current = current.BaseType)")
            .Line("{")
            .Line("    if (current.IsGenericType && current.GenericTypeArguments.Length == 1 && (current.GenericTypeArguments[0] == typeof(string) || current.GenericTypeArguments[0] == typeof(global::System.Guid)) &&")
            .Line("        (current.GetGenericTypeDefinition().FullName == \"Cratis.Concepts.ConceptAs`1\" || current.GetGenericTypeDefinition().FullName == \"Cratis.Chronicle.Events.EventSourceId`1\")) return true;")
            .Line("}")
            .Line("return false;")
            .EndBlock()
            .OpenBlock("public static string? Path(object? value, string path, bool uuidTarget = false)")
            .Line("foreach (var segment in path.Split('.'))")
            .Line("{")
            .Line("    if (value is null) return null;")
            .Line("    value = value.GetType().GetProperty(segment, global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.Public)?.GetValue(value);")
            .Line("}")
            .Line("return Value(value, uuidTarget);")
            .EndBlock()
            .OpenBlock("public static string? Query(global::Cratis.Arc.Authorization.AuthorizationPolicyContext context, string argument, string path, bool uuidTarget = false)")
            .Line("if (context.Target is not global::System.Reflection.MethodInfo method || !method.GetParameters().Any(parameter => string.Equals(parameter.Name, argument, global::System.StringComparison.Ordinal)) ||")
            .Line("    context.Resource is not global::Cratis.Arc.Queries.QueryContext { Arguments: { } arguments } || !arguments.TryGetValue(argument, out var key)) return null;")
            .Line("return path == argument ? Value(key, uuidTarget) : Path(key, path[(argument.Length + 1)..], uuidTarget);")
            .EndBlock()
            .EndBlock();

        // ESM policies are named rules without SemanticIds; the generated registrations realize protected operations.
        return new(Path.Combine("GeneratedPolicies", "Policies.cs"), builder.ToString())
        {
            Sources = [.. operations.Select(_ => _.Id)]
        };
    }

    static string Authorization(SemanticAuthorization authorization, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool command, string argument, string subject) => authorization switch
    {
        // A policy without negation keeps its two-valued rendering: without `not`, mapping unknown to false decides
        // exactly as three-valued logic followed by deny-on-unknown, so the generated bytes stay unchanged.
        SemanticPolicyReference reference => context.Application.Policies.Single(policy => policy.Name == reference.Name).Condition is var condition && Negates(condition)
            ? $"({Truth(condition, context, properties, command, argument, subject)} == true)"
            : Condition(context.Application.Policies.Single(policy => policy.Name == reference.Name).Condition, context, properties, command, argument, subject),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical => $"({Authorization(logical.Left, context, properties, command, argument, subject)} && {Authorization(logical.Right, context, properties, command, argument, subject)})",
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical => $"({Authorization(logical.Left, context, properties, command, argument, subject)} || {Authorization(logical.Right, context, properties, command, argument, subject)})",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticAuthorization), authorization.GetType().Name)
    };

    static string Condition(SemanticPolicyCondition condition, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool command, string argument, string subject) => condition switch
    {
        SemanticAuthenticatedCondition => "context.Principal.Identity?.IsAuthenticated == true",
        SemanticRoleCondition role => $"context.Principal.IsInRole({Literal(role.Role)})",
        SemanticClaimCondition claim => Claim(claim, context, properties, command, argument, subject),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical => $"({Condition(logical.Left, context, properties, command, argument, subject)} && {Condition(logical.Right, context, properties, command, argument, subject)})",
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical => $"({Condition(logical.Left, context, properties, command, argument, subject)} || {Condition(logical.Right, context, properties, command, argument, subject)})",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPolicyCondition), condition.GetType().Name)
    };

    static bool AllowsGuest(SemanticAuthorization authorization, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, string subject) => authorization switch
    {
        // Unknown denies at each named policy boundary, before composing the effective authorization.
        SemanticPolicyReference reference => GuestTruth(context.Application.Policies.Single(policy => policy.Name == reference.Name).Condition, context, properties, subject) == true,
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical => AllowsGuest(logical.Left, context, properties, subject) && AllowsGuest(logical.Right, context, properties, subject),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical => AllowsGuest(logical.Left, context, properties, subject) || AllowsGuest(logical.Right, context, properties, subject),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticAuthorization), authorization.GetType().Name)
    };

    // A guest has no authentication, roles or claims. A supported claim target can be supplied by the request,
    // so its comparison is false for that guest. Missing/null targets remain unknown in the runtime policy;
    // a statically non-text target is always unknown and cannot justify anonymous opt-in even under `not`.
    static bool? GuestTruth(SemanticPolicyCondition condition, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, string subject) => condition switch
    {
        SemanticAuthenticatedCondition or SemanticRoleCondition => false,
        SemanticClaimCondition claim => claim.TargetKind == SemanticClaimTargetKind.Literal ||
            SemanticClaimTargets.Primitive(context, SemanticClaimTargets.Property(context, properties, claim.TargetKind == SemanticClaimTargetKind.Subject ? subject : claim.Value!)) is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid ? false : null,
        SemanticNotPolicyCondition not => GuestTruth(not.Operand, context, properties, subject) is { } value ? !value : null,
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical => GuestTruth(logical.Left, context, properties, subject) & GuestTruth(logical.Right, context, properties, subject),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical => GuestTruth(logical.Left, context, properties, subject) | GuestTruth(logical.Right, context, properties, subject),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPolicyCondition), condition.GetType().Name)
    };

    static bool Negates(SemanticAuthorization authorization, IEnumerable<SemanticPolicy> policies) => authorization switch
    {
        SemanticPolicyReference reference => Negates(policies.Single(policy => policy.Name == reference.Name).Condition),
        SemanticLogicalAuthorization logical => Negates(logical.Left, policies) || Negates(logical.Right, policies),
        _ => false
    };

    static bool Negates(SemanticPolicyCondition condition) => condition switch
    {
        SemanticNotPolicyCondition => true,
        SemanticLogicalPolicyCondition logical => Negates(logical.Left) || Negates(logical.Right),
        _ => false
    };

    // Unknown is modeled explicitly as null and combined only through PolicyValues.Not/And/Or (Kleene logic),
    // never through Boolean negation or short-circuit operators.
    static string Truth(SemanticPolicyCondition condition, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool command, string argument, string subject) => condition switch
    {
        SemanticAuthenticatedCondition => "context.Principal.Identity?.IsAuthenticated == true",
        SemanticRoleCondition role => $"context.Principal.IsInRole({Literal(role.Role)})",
        SemanticClaimCondition claim => Claim(claim, context, properties, command, argument, subject, truth: true),
        SemanticNotPolicyCondition not => $"PolicyValues.Not({Truth(not.Operand, context, properties, command, argument, subject)})",
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical => $"PolicyValues.And({Truth(logical.Left, context, properties, command, argument, subject)}, {Truth(logical.Right, context, properties, command, argument, subject)})",
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical => $"PolicyValues.Or({Truth(logical.Left, context, properties, command, argument, subject)}, {Truth(logical.Right, context, properties, command, argument, subject)})",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticPolicyCondition), condition.GetType().Name)
    };

    static string Claim(SemanticClaimCondition claim, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool command, string argument, string subject, bool truth = false)
    {
        var primitive = claim.TargetKind == SemanticClaimTargetKind.Literal ? SemanticPrimitiveType.Text :
            SemanticClaimTargets.Primitive(context, SemanticClaimTargets.Property(context, properties, claim.TargetKind == SemanticClaimTargetKind.Subject ? subject : claim.Value!));

        // A number or Boolean is never a claim value. Two-valued rendering denies it outright; three-valued rendering
        // makes it unknown, so a negation over it can never allow, whatever a transport supplies at run time.
        if (primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber or SemanticPrimitiveType.Boolean)
        {
            return truth ? "null" : "false";
        }

        var uuidTarget = primitive == SemanticPrimitiveType.Uuid ? ", true" : string.Empty;
        var target = claim.TargetKind switch
        {
            SemanticClaimTargetKind.Literal => Literal(claim.Value!),
            SemanticClaimTargetKind.Artifact when command => $"PolicyValues.Path((context.Resource as global::Cratis.Arc.Commands.CommandContext)?.Command, {Literal(PascalPath(claim.Value!))}{uuidTarget})",
            SemanticClaimTargetKind.Artifact => $"PolicyValues.Query(context, {Literal(argument)}, {Literal(QueryPath(claim.Value!))}{uuidTarget})",
            SemanticClaimTargetKind.Subject when command => $"PolicyValues.Path((context.Resource as global::Cratis.Arc.Commands.CommandContext)?.Command, {Literal(PascalPath(subject))}{uuidTarget})",
            SemanticClaimTargetKind.Subject => $"PolicyValues.Query(context, {Literal(argument)}, {Literal(argument)}{uuidTarget})",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticClaimTargetKind), claim.TargetKind)
        };
        return $"PolicyValues.{(truth ? "Truth" : "Match")}(context, {Literal(claim.Claim)}, {target})";
    }

    static string PascalPath(string path) => string.Join('.', path.Split('.').Select(Identifiers.ToPascalCase));

    static string QueryPath(string path) => path.Split('.') is { Length: > 1 } parts
        ? $"{Identifiers.ToCamelCase(parts[0])}.{PascalPath(string.Join('.', parts.Skip(1)))}"
        : Identifiers.ToCamelCase(path);

    static string Literal(string value) => JsonSerializer.Serialize(value);
}
