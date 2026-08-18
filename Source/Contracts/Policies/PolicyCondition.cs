// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.Policies;

/// <summary>
/// Represents what a <see cref="PolicyDefinition"/> requires of the caller - the modeled <c>require</c> clause
/// of a policy.
/// </summary>
/// <remarks>
/// A tree rather than a flat list, for the same reason <see cref="AuthorizationRequirement"/> is one: a set of
/// checks cannot distinguish <c>A or B and C</c> from <c>(A or B) and C</c>, so a consumer deciding whether a
/// caller satisfies the policy could not answer from one.
/// <para>
/// Separate from <see cref="AuthorizationRequirement"/> even though both combine with <c>and</c> and <c>or</c>,
/// because the leaves are different things: an authorization requirement's leaf names a policy, and a policy
/// condition's leaf is a check against the caller. Merging them would let a policy name a policy, which the
/// language does not allow.
/// </para>
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(AuthenticatedCondition), "authenticated")]
[JsonDerivedType(typeof(RoleCondition), "role")]
[JsonDerivedType(typeof(ClaimCondition), "claim")]
[JsonDerivedType(typeof(LogicalPolicyCondition), "logical")]
public abstract record PolicyCondition;

/// <summary>
/// Represents the <c>authenticated</c> condition - the caller must be signed in, and nothing more is required
/// of them.
/// </summary>
public record AuthenticatedCondition : PolicyCondition;

/// <summary>
/// Represents a <c>role "&lt;name&gt;"</c> condition - the caller must hold the named role.
/// </summary>
/// <param name="Role">The name of the required role.</param>
public record RoleCondition(string Role) : PolicyCondition;

/// <summary>
/// Represents a <c>claim "&lt;name&gt;" matches &lt;target&gt;</c> condition - the caller must carry the named
/// claim, with the value the target names.
/// </summary>
/// <param name="Claim">The name of the claim the caller must carry.</param>
/// <param name="MatchesSubject">Whether the claim is matched against the subject rather than against
/// <paramref name="Value"/>.</param>
/// <param name="ValueKind">Where the value the claim is matched against comes from, when
/// <paramref name="MatchesSubject"/> is <see langword="false"/>.</param>
/// <param name="Value">The value the claim is matched against, interpreted according to
/// <paramref name="ValueKind"/>. Empty when the claim is matched against the subject.</param>
/// <remarks>
/// The target keeps its <see cref="ProducedValueKind"/> rather than collapsing to text, so a literal value
/// stays distinguishable from something the application resolves while it runs - the same distinction
/// <see cref="ProducedEventProperty"/> keeps for a produced event's property.
/// </remarks>
public record ClaimCondition(
    string Claim,
    bool MatchesSubject,
    ProducedValueKind ValueKind,
    string Value) : PolicyCondition;

/// <summary>
/// Represents two policy conditions combined with <c>and</c> or <c>or</c>.
/// </summary>
/// <param name="Left">The left hand condition.</param>
/// <param name="Operator">The operator combining the conditions.</param>
/// <param name="Right">The right hand condition.</param>
public record LogicalPolicyCondition(
    PolicyCondition Left,
    ProducedEventLogicalOperator Operator,
    PolicyCondition Right) : PolicyCondition;
