// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Stage.Contracts.Commands;

/// <summary>
/// Represents what a command's <c>authorize</c> declaration requires of the caller — a policy, or policies combined.
/// </summary>
/// <remarks>
/// A tree rather than a flat list of policy names, deliberately. A set of names cannot distinguish
/// <c>A or B and C</c> from <c>(A or B) and C</c>, so a consumer deciding whether a caller is allowed cannot
/// answer from one — which is what <see href="https://github.com/Cratis/Screenplay/issues/68">Screenplay#68</see>
/// was about. Use <see cref="Policies"/> for the flat set when only the names are needed.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(PolicyReference), "policy")]
[JsonDerivedType(typeof(LogicalRequirement), "logical")]
public abstract record AuthorizationRequirement
{
    /// <summary>
    /// Gets every policy the requirement names, in the order they appear.
    /// </summary>
    /// <returns>The names of the policies held anywhere in the requirement.</returns>
    /// <remarks>
    /// Derived from the tree rather than stored beside it, for a consumer that only needs to know which policies
    /// are involved — resolving them, listing them — and not how they combine.
    /// </remarks>
    public IEnumerable<string> Policies() =>
        this switch
        {
            PolicyReference reference => [reference.Policy],
            LogicalRequirement logical => logical.Left.Policies().Concat(logical.Right.Policies()),
            _ => []
        };
}

/// <summary>
/// Represents a reference to a single named policy the caller must satisfy.
/// </summary>
/// <param name="Policy">The name of the referenced policy.</param>
public record PolicyReference(string Policy) : AuthorizationRequirement;

/// <summary>
/// Represents two authorization requirements combined with <c>and</c> or <c>or</c>.
/// </summary>
/// <param name="Left">The left hand requirement.</param>
/// <param name="Operator">The operator combining the requirements.</param>
/// <param name="Right">The right hand requirement.</param>
public record LogicalRequirement(
    AuthorizationRequirement Left,
    ProducedEventLogicalOperator Operator,
    AuthorizationRequirement Right) : AuthorizationRequirement;
