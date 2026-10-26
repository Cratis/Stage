// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies;

/// <summary>
/// Describes operations whose different policy bindings cannot have distinct generated class names.
/// </summary>
/// <param name="Operations">The conflicting operation identities.</param>
/// <param name="PolicyName">The generated policy name shared by the operations.</param>
internal sealed record ConflictingPolicyBindings(ImmutableArray<SemanticId> Operations, string PolicyName)
{
    /// <summary>
    /// The diagnostic code for conflicting policy bindings.
    /// </summary>
    public const string Code = "STAGE-AUTH-002";

    /// <summary>
    /// Gets the conflict diagnostic message.
    /// </summary>
    public string Message => $"Operations {string.Join(", ", Operations)} bind the policy '{PolicyName}' differently and cannot be given distinct policy class names.";
}
