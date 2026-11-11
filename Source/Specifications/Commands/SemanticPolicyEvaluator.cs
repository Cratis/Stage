// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications.Commands;

internal static class SemanticPolicyEvaluator
{
    internal const string SystemAuthentication = "stage-system";

    internal static ClaimsPrincipal Principal(SemanticCaller caller) => Build(caller, "stage");

    internal static ClaimsPrincipal SystemPrincipal(SemanticCaller caller) => Build(caller, SystemAuthentication);

    internal static bool Allows(SemanticAuthorization? authorization, SemanticExecutionPlan plan, SemanticCaller? caller, ClaimsPrincipal principal, SemanticCommand command, SemanticSpecification specification, Func<SemanticId, IEnumerable<SemanticReadModelInstance>>? instances = null)
    {
        if (authorization is null || SemanticAnonymousAccess.IsAnonymous(authorization, plan.Model.Application.Policies)) return true;
        if (SemanticAnonymousAccess.NestsAnonymous(authorization, plan.Model.Application.Policies)) return false;
        if (caller is not { Roles.IsDefault: false, Claims.IsDefault: false }) return false;
        var values = specification.When!.Values;
        var artifact = command.Properties.Select(property => (property, value: values.FirstOrDefault(value => value.TargetProperty == property.Id)?.Value))
            .Where(pair => pair.value is not null).ToDictionary(pair => pair.property.Name, pair => pair.value!, StringComparer.Ordinal);
        var subject = command.Properties.Where(property => property.IsIdentifier)
            .Select(property => values.FirstOrDefault(value => value.TargetProperty == property.Id)?.Value).FirstOrDefault(value => value is not null);
        return Authorization(authorization, plan, caller, principal, artifact, subject, command.Properties, instances);
    }

    internal static bool Allows(SemanticKeyedQuery query, SemanticValue key, SemanticExecutionPlan plan, SemanticCaller? caller, Func<SemanticId, IEnumerable<SemanticReadModelInstance>>? instances = null)
    {
        if (query.Authorization is null || SemanticAnonymousAccess.IsAnonymous(query.Authorization, plan.Model.Application.Policies)) return true;
        if (SemanticAnonymousAccess.NestsAnonymous(query.Authorization, plan.Model.Application.Policies)) return false;
        if (caller is not { Roles.IsDefault: false, Claims.IsDefault: false }) return false;
        var artifact = query.Argument is null
            ? [with(comparer: StringComparer.Ordinal)]
            : new Dictionary<string, SemanticValue>(StringComparer.Ordinal) { [query.Argument.Name] = key };
        var properties = query.Argument is null ? Array.Empty<SemanticProperty>()
            : [new SemanticProperty(query.Argument.Id, query.Argument.Name, query.Argument.Type, false)];

        return Authorization(query.Authorization, plan, caller, Principal(caller), artifact, key, properties, instances);
    }

    static ClaimsPrincipal Build(SemanticCaller caller, string authentication)
    {
        const string roleClaim = ClaimTypes.Role;
        var claims = caller.Roles.Select(role => new Claim(roleClaim, role))
            .Concat(caller.Claims.Select(claim => new Claim(claim.Type, claim.Value)));

        return new(new ClaimsIdentity(claims, caller.Authenticated ? authentication : null, ClaimTypes.Name, roleClaim));
    }

    // A policy that reads recorded state decides from the instances the specification gave and the facts have projected. A read that finds no instance
    // denies: state that is not recorded never allows, as in Screenplay's reference evaluator.
    static bool PolicyAllows(SemanticPolicy policy, SemanticPolicyReference reference, SemanticExecutionPlan plan, SemanticCaller caller, ClaimsPrincipal principal, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticValue? subject, IEnumerable<SemanticProperty> properties, Func<SemanticId, IEnumerable<SemanticReadModelInstance>>? instances)
    {
        if (policy.Condition is SemanticOpaquePolicyCondition)
        {
            throw new OpaquePolicyReached(reference.Name);
        }

        IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? state = null;
        if (!policy.Reads.IsDefaultOrEmpty)
        {
            state = ReadState(policy, reference, artifact, instances);
            if (state is null)
            {
                return false;
            }
        }

        if (UsesCallerId(policy.Condition) && string.IsNullOrEmpty(caller.Id))
        {
            // A caller that cannot say who it is gives nothing to compare, so the policy is unavailable and denies, as the generated policy does.
            return false;
        }

        return Condition(WithCallerId(policy.Condition, caller.Id), plan, caller, principal, artifact, subject, properties, state) == true;
    }

    static Dictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? ReadState(SemanticPolicy policy, SemanticPolicyReference reference, IReadOnlyDictionary<string, SemanticValue> artifact, Func<SemanticId, IEnumerable<SemanticReadModelInstance>>? instances)
    {
        var state = new Dictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>(StringComparer.Ordinal);
        foreach (var read in policy.Reads)
        {
            var source = reference.Mappings.IsDefaultOrEmpty ? read.By : reference.Mappings.FirstOrDefault(mapping => mapping.Input == read.By)?.Property ?? read.By;
            if (instances is null || !artifact.TryGetValue(source, out var key) || IsRefused(key))
            {
                return null;
            }

            var instance = instances(read.ReadModel).FirstOrDefault(candidate => SemanticExpectationComparer.AreEqual(candidate.Key, key));
            if (instance is null)
            {
                return null;
            }

            state[read.Name] = instance.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
        }

        return state;
    }

    static bool UsesCallerId(SemanticPolicyCondition condition) => condition switch
    {
        SemanticStatePolicyCondition state => UsesCallerId(state.Condition),
        SemanticNotPolicyCondition not => UsesCallerId(not.Operand),
        SemanticLogicalPolicyCondition logical => UsesCallerId(logical.Left) || UsesCallerId(logical.Right),
        _ => false
    };

    static bool UsesCallerId(SemanticCondition condition) => condition switch
    {
        SemanticComparison comparison => comparison.Left.CallerId || comparison.Right.CallerId,
        SemanticLogicalCondition logical => UsesCallerId(logical.Left) || UsesCallerId(logical.Right),
        _ => false
    };

    static SemanticPolicyCondition WithCallerId(SemanticPolicyCondition condition, string? id) => condition switch
    {
        SemanticStatePolicyCondition state => state with { Condition = WithCallerId(state.Condition, id) },
        SemanticNotPolicyCondition not => not with { Operand = WithCallerId(not.Operand, id) },
        SemanticLogicalPolicyCondition logical => logical with { Left = WithCallerId(logical.Left, id), Right = WithCallerId(logical.Right, id) },
        _ => condition
    };

    static SemanticCondition WithCallerId(SemanticCondition condition, string? id) => condition switch
    {
        SemanticComparison comparison => comparison with { Left = CallerId(comparison.Left, id), Right = CallerId(comparison.Right, id) },
        SemanticLogicalCondition logical => logical with { Left = WithCallerId(logical.Left, id), Right = WithCallerId(logical.Right, id) },
        _ => condition
    };

    static SemanticConditionOperand CallerId(SemanticConditionOperand operand, string? id) =>
        operand.CallerId ? new SemanticConditionOperand(default, id is null ? SemanticValue.Null : SemanticValue.Text(id)) : operand;

    // Decision 0091: a refused key is missing even if prior facts project an instance at that key.
    static bool IsRefused(SemanticValue key) => key switch
    {
        SemanticTextValue text => string.IsNullOrWhiteSpace(text.Value) || text.Value == "*" ||
            text.Value.Contains('#', StringComparison.Ordinal) || char.IsWhiteSpace(text.Value[0]) || char.IsWhiteSpace(text.Value[^1]),
        SemanticCompositeValue composite => composite.Properties.Any(property => IsRefused(property.Value)),
        _ => false
    };

    static bool Authorization(SemanticAuthorization authorization, SemanticExecutionPlan plan, SemanticCaller caller, ClaimsPrincipal principal, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticValue? subject, IEnumerable<SemanticProperty> properties, Func<SemanticId, IEnumerable<SemanticReadModelInstance>>? instances) => authorization switch
    {
        // As in Screenplay's reference evaluator, only a definite true allows: an unknown policy result denies.
        SemanticPolicyReference reference => PolicyAllows(plan.Model.Application.Policies.Single(policy => policy.Name == reference.Name), reference, plan, caller, principal, artifact, subject, properties, instances),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.And } logical => Authorization(logical.Left, plan, caller, principal, artifact, subject, properties, instances) && Authorization(logical.Right, plan, caller, principal, artifact, subject, properties, instances),
        SemanticLogicalAuthorization { Operator: SemanticLogicalOperator.Or } logical => Authorization(logical.Left, plan, caller, principal, artifact, subject, properties, instances) || Authorization(logical.Right, plan, caller, principal, artifact, subject, properties, instances),
        _ => throw new UnsupportedSemanticMapping()
    };

    // Three-valued (Kleene) policy logic over bool?: null is unknown, and/or decide on a definite operand in either
    // order, and not keeps unknown unknown. Plain Boolean negation would turn a negated unknown claim comparison into an allow.
    static bool? Condition(SemanticPolicyCondition condition, SemanticExecutionPlan plan, SemanticCaller caller, ClaimsPrincipal principal, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticValue? subject, IEnumerable<SemanticProperty> properties, IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? state) => condition switch
    {
        SemanticAnonymousCondition => true,
        SemanticAuthenticatedCondition => principal.Identity?.IsAuthenticated == true,
        SemanticRoleCondition role => principal.IsInRole(role.Role),
        SemanticClaimCondition claim => principal.Identity?.AuthenticationType == SystemAuthentication ? null : ClaimMatches(claim, plan, caller, artifact, subject, properties),
        SemanticNotPolicyCondition not => Not(Condition(not.Operand, plan, caller, principal, artifact, subject, properties, state)),
        SemanticStatePolicyCondition recorded => state is not null && SemanticConditionEvaluator.Evaluate(recorded.Condition, new Dictionary<SemanticId, SemanticValue>(), null, state),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.And } logical => And(Condition(logical.Left, plan, caller, principal, artifact, subject, properties, state), Condition(logical.Right, plan, caller, principal, artifact, subject, properties, state)),
        SemanticLogicalPolicyCondition { Operator: SemanticLogicalOperator.Or } logical => Or(Condition(logical.Left, plan, caller, principal, artifact, subject, properties, state), Condition(logical.Right, plan, caller, principal, artifact, subject, properties, state)),
        _ => throw new UnsupportedSemanticMapping()
    };

    static bool? Not(bool? value) => value is null ? null : !value.Value;

    static bool? And(bool? left, bool? right) => (left, right) switch
    {
        (false, _) or (_, false) => false,
        (true, true) => true,
        _ => null
    };

    static bool? Or(bool? left, bool? right) => (left, right) switch
    {
        (true, _) or (_, true) => true,
        (false, false) => false,
        _ => null
    };

    // A missing, null, absent-subject or non-text target makes the comparison unknown rather than false.
    static bool? ClaimMatches(SemanticClaimCondition claim, SemanticExecutionPlan plan, SemanticCaller caller, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticValue? subject, IEnumerable<SemanticProperty> properties)
    {
        var target = claim.TargetKind switch
        {
            SemanticClaimTargetKind.Literal => claim.Value,
            SemanticClaimTargetKind.Subject => (subject as SemanticTextValue)?.Value,
            SemanticClaimTargetKind.Artifact when claim.Value is not null => (ArtifactValue(claim.Value, artifact, plan, properties) as SemanticTextValue)?.Value,
            _ => null
        };
        if (target is null) return null;

        return caller.Claims.Any(value =>
            string.Equals(value.Type, claim.Claim, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(value.Value, target, StringComparison.Ordinal));
    }

    static SemanticValue? ArtifactValue(string path, IReadOnlyDictionary<string, SemanticValue> artifact, SemanticExecutionPlan plan, IEnumerable<SemanticProperty> properties)
    {
        var parts = path.Split('.');
        if (!artifact.TryGetValue(parts[0], out var value)) return null;
        var type = properties.Single(property => property.Name == parts[0]).Type;
        foreach (var name in parts.Skip(1))
        {
            if (value is not SemanticCompositeValue composite || type.Kind != SemanticTypeReferenceKind.CompositeType) return null;
            var shape = plan.Model.Application.Types.Single(declaration => declaration.Id == type.Target);
            var member = shape.Properties.Single(property => property.Name == name);
            value = composite.Properties.SingleOrDefault(property => property.TargetProperty == member.Id)?.Value;
            if (value is null) return null;
            type = member.Type;
        }

        return value;
    }
}

// A private control signal must not become part of the Specifications assembly's public exception API.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "The authorization signal is internal to the specification executor.")]
internal sealed class OpaquePolicyReached(string policy) : Exception($"Policy '{policy}' requires a target to evaluate its implementation body.");
