// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Commands;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts a Screenplay <see cref="AuthorizeSyntax"/> into the Stage <see cref="AuthorizationRequirement"/> tree.
/// </summary>
/// <remarks>
/// The tree is carried whole rather than flattened to the policy names it mentions. Flattening cannot distinguish
/// <c>A or B and C</c> from <c>(A or B) and C</c>, so a consumer deciding whether a caller is allowed could not
/// answer from the result — see <see href="https://github.com/Cratis/Screenplay/issues/68">Screenplay#68</see>.
/// A consumer that only wants the names calls <see cref="AuthorizationRequirement.Policies"/>.
/// </remarks>
public static class AuthorizationConverter
{
    /// <summary>
    /// Converts an authorize declaration into its Stage requirement.
    /// </summary>
    /// <param name="authorize">The declaration, or <see langword="null"/> when the construct declares none.</param>
    /// <returns>The Stage requirement, or <see langword="null"/> when nothing is required.</returns>
    public static AuthorizationRequirement? Convert(AuthorizeSyntax? authorize) =>
        authorize is null ? null : Convert(authorize.Requirement);

    static AuthorizationRequirement? Convert(PolicyRequirementSyntax requirement) =>
        requirement switch
        {
            PolicyReferenceSyntax reference => new PolicyReference(reference.Name),
            LogicalPolicyRequirementSyntax logical when Convert(logical.Left) is { } left && Convert(logical.Right) is { } right =>
                new LogicalRequirement(left, ConditionConverter.Operator(logical.Operator), right),
            _ => null
        };
}
