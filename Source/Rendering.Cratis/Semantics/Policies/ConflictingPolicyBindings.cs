// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies;

/// <summary>
/// Signals operations whose different policy bindings cannot have distinct generated class names.
/// </summary>
/// <param name="operations">The conflicting operation identities.</param>
/// <param name="policyName">The generated policy name shared by the operations.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Only internal policy naming throws this diagnostic exception; it is not a public API.")]
internal sealed class ConflictingPolicyBindings(ImmutableArray<SemanticId> operations, string policyName)
    : Exception($"Operations {string.Join(", ", operations)} bind the policy '{policyName}' differently and cannot be given distinct policy class names.")
{
    /// <summary>
    /// The diagnostic code for conflicting policy bindings.
    /// </summary>
    public const string Code = "STAGE-AUTH-002";

    /// <summary>
    /// Gets the conflicting operation identities.
    /// </summary>
    public ImmutableArray<SemanticId> Operations { get; } = operations;
}
