// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Authorization;

/// <summary>
/// Renders a Screenplay <c>authorize</c> declaration as the Cratis Arc authorization attribute the generated
/// artifact carries.
/// </summary>
/// <remarks>
/// <para>
/// An <c>authorize</c> names <c>policy</c> declarations, optionally several as alternatives (<c>or</c>), so the
/// declaration is a disjunction: a caller satisfying any one of them is authorized. Arc's <c>[Roles]</c> has the
/// same shape — it grants on <b>any one</b> of the roles it lists — so a disjunction of role policies renders as a
/// single <c>[Roles]</c> carrying their union, and <c>require authenticated</c> renders as a bare
/// <c>[Authorize]</c>. An alternative that only requires authentication subsumes every role alternative beside it,
/// which is why it collapses the whole declaration to <c>[Authorize]</c>. No <c>authorize</c> at all renders as
/// <c>[AllowAnonymous]</c>, stating the absence rather than leaving it to be inferred.
/// </para>
/// <para>
/// What an attribute cannot express is reported rather than approximated silently: a policy declared nowhere, a
/// policy whose requirement is authored code, a <c>claim</c> requirement, and — importantly — a <b>conjunction</b>
/// of requirements, because <c>[Roles]</c> means any-of and Arc evaluates no other attribute member (its
/// <c>Policy</c> property is carried but never read). Those fall back to requiring an authenticated caller, which
/// is weaker than declared but never anonymous, and each one says so.
/// </para>
/// </remarks>
public static class AuthorizationRenderer
{
    /// <summary>
    /// The namespace the rendered authorization attributes live in.
    /// </summary>
    public const string Namespace = "Cratis.Arc.Authorization";

    /// <summary>
    /// Renders the authorization attribute for a single <c>authorize</c> declaration.
    /// </summary>
    /// <param name="authorize">The declaration, or <see langword="null"/> when the construct declares none.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> the policies are resolved against.</param>
    /// <param name="subject">What is being authorized, for diagnostics (for example <c>Command 'Invite'</c>).</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The attribute content, without the surrounding brackets.</returns>
    public static string Render(AuthorizeSyntax? authorize, ApplicationSet applicationSet, string subject, ICollection<string> diagnostics) =>
        Render([authorize], applicationSet, subject, diagnostics);

    /// <summary>
    /// Renders the single authorization attribute covering several <c>authorize</c> declarations at once — the
    /// union of what they permit, for an artifact that stands in for all of them.
    /// </summary>
    /// <param name="authorizations">The declarations; an entry is <see langword="null"/> when its construct declares none.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> the policies are resolved against.</param>
    /// <param name="subject">What is being authorized, for diagnostics (for example <c>Read model 'Invoice'</c>).</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The attribute content, without the surrounding brackets.</returns>
    public static string Render(
        IEnumerable<AuthorizeSyntax?> authorizations, ApplicationSet applicationSet, string subject, ICollection<string> diagnostics)
    {
        var declared = authorizations.ToArray();
        if (declared.Length == 0 || declared.Any(authorize => authorize?.Policies.Any() != true))
        {
            return "AllowAnonymous";
        }

        var required = declared
            .SelectMany(authorize => authorize!.Policies)
            .Select(reference => RolesRequiredBy(reference, applicationSet, subject, diagnostics))
            .ToArray();

        if (required.Any(roles => roles.Length == 0))
        {
            return "Authorize";
        }

        var union = required.SelectMany(roles => roles).Distinct(StringComparer.Ordinal).Select(CSharpCodeBuilder.StringLiteral);
        return $"Roles({string.Join(", ", union)})";
    }

    /// <summary>
    /// Resolves the roles one referenced policy requires. An empty result means "any authenticated caller" — both
    /// when that is what the policy says and when its requirement has no attribute equivalent, in which case a
    /// diagnostic records that the rendered attribute is weaker than the document.
    /// </summary>
    /// <param name="reference">The policy reference to resolve.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> the policy is resolved against.</param>
    /// <param name="subject">What is being authorized, for diagnostics.</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The required roles, any one of which grants access; empty for authentication alone.</returns>
    static string[] RolesRequiredBy(
        PolicyReferenceSyntax reference, ApplicationSet applicationSet, string subject, ICollection<string> diagnostics)
    {
        if (!applicationSet.Policies.TryGetValue(reference.Name, out var policy))
        {
            diagnostics.Add(
                $"{subject} authorizes against policy '{reference.Name}', which nothing declares — " +
                "rendered as requiring an authenticated caller.");
            return [];
        }

        if (policy.Code is not null)
        {
            diagnostics.Add(
                $"{subject} authorizes against policy '{policy.Name}', whose requirement is a {policy.Code.Language} block with no " +
                "attribute equivalent — rendered as requiring an authenticated caller.");
            return [];
        }

        var roles = RolesOf(policy.Condition);
        if (roles is null)
        {
            diagnostics.Add(
                $"{subject} authorizes against policy '{policy.Name}', which requires {Describe(policy.Condition)} — no authorization " +
                "attribute expresses that, so it is rendered as requiring an authenticated caller.");
            return [];
        }

        return roles;
    }

    /// <summary>
    /// Reduces a policy condition to the roles an attribute can carry, or <see langword="null"/> when no attribute
    /// expresses it.
    /// </summary>
    /// <param name="condition">The condition to reduce.</param>
    /// <returns>The roles, empty for authentication alone, or <see langword="null"/> when inexpressible.</returns>
    static string[]? RolesOf(PolicyConditionSyntax? condition) => condition switch
    {
        AuthenticatedConditionSyntax => [],
        RoleConditionSyntax role => [role.Role],
        LogicalPolicyConditionSyntax { Operator: LogicalOperator.Or } logical => Union(RolesOf(logical.Left), RolesOf(logical.Right)),
        _ => null,
    };

    static string[]? Union(string[]? left, string[]? right)
    {
        if (left is null || right is null)
        {
            return null;
        }

        // An alternative satisfied by authentication alone subsumes the roles beside it.
        if (left.Length == 0 || right.Length == 0)
        {
            return [];
        }

        return [.. left.Concat(right).Distinct(StringComparer.Ordinal)];
    }

    static string Describe(PolicyConditionSyntax? condition) => condition switch
    {
        null => "nothing",
        ClaimConditionSyntax claim => $"the claim '{claim.Claim}'",
        LogicalPolicyConditionSyntax { Operator: LogicalOperator.And } => "more than one thing at once",
        _ => "a requirement this renderer does not understand",
    };
}
