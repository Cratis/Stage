// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies;

/// <summary>
/// Renders a policy that decides from recorded state (ESM v44) as a fail-closed Arc authorization policy.
/// </summary>
/// <remarks>
/// The generated policy reads each declared read model by the key the authorized operation supplies. An absent key,
/// an absent instance and a failing read all make that policy unavailable, and an unavailable policy denies, as the
/// reference evaluator does. A policy that is unavailable never becomes <see langword="false"/> under negation, so absence cannot allow.
/// </remarks>
internal static class SemanticStatePolicyRendering
{
    internal static SemanticPolicy? Find(SemanticApplicationContext context, SemanticPolicyReference reference) =>
        context.Application.Policies.SingleOrDefault(policy => policy.Name == reference.Name) is { } found && !found.Reads.IsDefaultOrEmpty ? found : null;

    internal static IReadOnlyList<(SemanticPolicy Policy, SemanticPolicyReference Reference)> Referenced(SemanticAuthorization? authorization, SemanticApplicationContext context)
    {
        var result = new List<(SemanticPolicy, SemanticPolicyReference)>();
        Collect(authorization, context, result);

        return [.. result.DistinctBy(item => Prefix(item.Item2))];
    }

    // The caller's identifier as the generated policy reads it. A policy that uses it is unavailable when the principal carries none, so the
    // null-forgiving access here is never reached with a missing identifier.
    internal static string CallerId(SemanticApplicationContext context) => $"{PolicyValuesName(context)}.CallerId(context)!";

    // The claims a specification's caller carries, as C# expressions: its roles, its claims and, when it names one, its identifier as the name identifier.
    internal static IEnumerable<string> CallerClaims(SemanticCaller caller)
    {
        var claims = caller.Roles.Select(role => $"new global::System.Security.Claims.Claim(global::System.Security.Claims.ClaimTypes.Role, {CSharpCodeBuilder.StringLiteral(role)})")
            .Concat(caller.Claims.Select(claim => $"new global::System.Security.Claims.Claim({CSharpCodeBuilder.StringLiteral(claim.Type)}, {CSharpCodeBuilder.StringLiteral(claim.Value)})"));

        return caller.Id is { } id
            ? claims.Append($"new global::System.Security.Claims.Claim(global::System.Security.Claims.ClaimTypes.NameIdentifier, {CSharpCodeBuilder.StringLiteral(id)})")
            : claims;
    }

    internal static bool UsesCallerId(SemanticPolicy policy) =>
        Leaves(policy.Condition).Any(leaf => UsesCallerId(leaf.Condition));

    internal static string Prefix(SemanticPolicyReference reference) =>
        Identifiers.ToCamelCase(reference.Name) + string.Concat(reference.Mappings.Select(mapping => $"_{mapping.Input}_{mapping.Property.Replace('.', '_')}"));

    internal static string Available(SemanticPolicyReference reference) => Identifiers.EscapeKeyword($"{Prefix(reference)}Available");

    internal static SemanticReadScope Scope(SemanticPolicy policy, SemanticPolicyReference reference, SemanticApplicationContext context) =>
        new(policy, context, alias => $"{Prefix(reference)}{Identifiers.ToPascalCase(alias)}");

    internal static string Leaf(SemanticStatePolicyCondition state, SemanticApplicationContext context, SemanticReadScope scope) =>
        SemanticConditionRendering.Render(state.Condition, new([], string.Empty, context, Reads: scope));

    internal static string Source(SemanticPolicyReference reference, SemanticPolicyRead read) =>
        reference.Mappings.IsDefaultOrEmpty ? read.By : reference.Mappings.FirstOrDefault(mapping => mapping.Input == read.By)?.Property ?? read.By;

    internal static bool CanRender(SemanticStatePolicyCondition state, SemanticApplicationContext context, SemanticReadScope scope) =>
        state.Condition switch
        {
            SemanticComparison { Operator: SemanticComparisonOperator.Contains or SemanticComparisonOperator.StartsWith } comparison =>
                comparison.Left.Read is not null && scope.Property(comparison.Left.Read, comparison.Left.Property) is { } property &&
                comparison.Right.Value is SemanticTextValue && !property.Type.IsOptional,
            { } condition => SemanticConditionRendering.CanRender(condition, [], context, reads: scope)
        };

    internal static IEnumerable<SemanticStatePolicyCondition> Leaves(SemanticPolicyCondition condition) => condition switch
    {
        SemanticStatePolicyCondition state => [state],
        SemanticNotPolicyCondition not => Leaves(not.Operand),
        SemanticLogicalPolicyCondition logical => Leaves(logical.Left).Concat(Leaves(logical.Right)),
        _ => []
    };

    internal static string Term(
        SemanticPolicy policy,
        SemanticPolicyReference reference,
        SemanticApplicationContext context,
        IReadOnlyList<SemanticProperty> properties,
        bool command,
        string argument,
        string subject,
        bool naming)
    {
        var scope = Scope(policy, reference, context);
        var inner = SemanticPolicyArtifactRenderer.Negates(policy.Condition)
            ? $"({SemanticPolicyArtifactRenderer.Truth(policy.Condition, context, properties, command, argument, subject, scope)} == true)"
            : SemanticPolicyArtifactRenderer.Condition(policy.Condition, context, properties, command, argument, subject, scope);
        var keys = naming ? $"[{string.Join(';', policy.Reads.Select(read => Key(read, reference, context, properties, command, argument)))}]" : string.Empty;

        return $"({Available(reference)}{keys} && {inner})";
    }

    internal static void Preamble(
        CSharpCodeBuilder builder,
        SemanticPolicy policy,
        SemanticPolicyReference reference,
        SemanticApplicationContext context,
        IReadOnlyList<SemanticProperty> properties,
        bool command,
        string argument)
    {
        var scope = Scope(policy, reference, context);
        var types = new SemanticTypeSystem(context);
        var available = Available(reference);
        builder.Line($"var {available} = true;");
        if (UsesCallerId(policy))
        {
            builder.Line($"if ({PolicyValuesName(context)}.CallerId(context) is null) {available} = false;");
        }

        foreach (var read in policy.Reads)
        {
            var view = context.ReadModels[read.ReadModel];
            var type = types.SliceType(view.Id, view.Name);
            var local = scope.Local(read.Name);
            builder.Line($"{type} {local} = null!;")
                .OpenBlock("try")
                .Line($"var key = {Key(read, reference, context, properties, command, argument)};")
                .Line($"if (key is not null) {local} = await readModels.GetInstanceById<{type}>(key);")
                .EndBlock()
                .OpenBlock("catch (global::System.Exception exception) when (!(exception is global::System.OperationCanceledException && cancellationToken.IsCancellationRequested))")
                .Line($"{available} = false;")
                .EndBlock()
                .Line($"if (global::System.Object.ReferenceEquals({local}, null)) {available} = false;");
        }
    }

    internal static string Key(SemanticPolicyRead read, SemanticPolicyReference reference, SemanticApplicationContext context, IReadOnlyList<SemanticProperty> properties, bool command, string argument)
    {
        var source = Source(reference, read);
        var primitive = SemanticClaimTargets.Primitive(context, SemanticClaimTargets.Property(context, properties, source));
        var targetType = primitive switch
        {
            SemanticPrimitiveType.Uuid => ", true",
            SemanticPrimitiveType.Date => ", dateTarget: true",
            SemanticPrimitiveType.DateTime => ", dateTimeTarget: true",
            _ => string.Empty
        };
        var values = PolicyValuesName(context);

        return command
            ? $"{values}.Path((context.Resource as global::Cratis.Arc.Commands.CommandContext)?.Command, {System.Text.Json.JsonSerializer.Serialize(context.MemberNames.PropertyPath(properties, source))}{targetType})"
            : $"{values}.Query(context, {System.Text.Json.JsonSerializer.Serialize(argument)}, {System.Text.Json.JsonSerializer.Serialize(QueryPath(source))}{targetType})";
    }

    static string QueryPath(string path) => path.Split('.') is { Length: > 1 } parts
        ? $"{Identifiers.ToCamelCase(parts[0])}.{string.Join('.', parts.Skip(1).Select(Identifiers.ToPascalCase))}"
        : Identifiers.ToCamelCase(path);

    static bool UsesCallerId(SemanticCondition condition) => condition switch
    {
        SemanticComparison comparison => comparison.Left.CallerId || comparison.Right.CallerId,
        SemanticLogicalCondition logical => UsesCallerId(logical.Left) || UsesCallerId(logical.Right),
        _ => false
    };

    static string PolicyValuesName(SemanticApplicationContext context) =>
        context.Domain.Count == 0 ? "PolicyValues" : $"global::{context.RootNamespace}.GeneratedPolicies.PolicyValues";

    static void Collect(SemanticAuthorization? authorization, SemanticApplicationContext context, List<(SemanticPolicy, SemanticPolicyReference)> result)
    {
        switch (authorization)
        {
            case SemanticPolicyReference reference when Find(context, reference) is { } policy:
                result.Add((policy, reference));
                break;
            case SemanticLogicalAuthorization logical:
                Collect(logical.Left, context, result);
                Collect(logical.Right, context, result);
                break;
        }
    }
}
