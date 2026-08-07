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
/// Every referenced policy is reduced to the roles it happens to require, and those roles are unioned into a
/// single <c>[Roles]</c>. <c>require authenticated</c> renders as a bare <c>[Authorize]</c>, and an alternative
/// satisfied by authentication alone subsumes the roles beside it. No <c>authorize</c> at all renders as
/// <c>[AllowAnonymous]</c>, stating the absence rather than leaving it to be inferred. A policy declared nowhere,
/// a policy whose requirement is authored code, and a <c>claim</c> requirement have no attribute equivalent; each
/// falls back to requiring an authenticated caller and says so.
/// </para>
/// <para>
/// <b>This is known to be wrong, and is preserved here deliberately</b> — see
/// <see href="https://github.com/Cratis/Stage/issues/20">Cratis/Stage#20</see>. Policies and roles are not the
/// same shape: a role says <i>which callers are allowed</i>, so a set of roles is naturally an <b>any-of</b>;
/// a policy is a <i>demand</i>, so a set of policies is naturally an <b>all-of</b>. Reducing a policy to its
/// roles erases that distinction, and the union then grants on any one of them. So <c>authorize A B</c> — which
/// the document means as <i>both</i> — renders as <c>[Roles("A", "B")]</c>, which Arc evaluates as <i>either</i>.
/// The rendered application is more permissive than the document it came from.
/// </para>
/// <para>
/// The fix is to render policies <i>as policies</i> rather than as the roles behind them, which this renderer
/// cannot do yet: Arc carries <c>AuthorizeAttribute.Policy</c> but never evaluates it
/// (<see href="https://github.com/Cratis/Arc/issues/2464">Cratis/Arc#2464</see>), so a policy-named attribute
/// would admit any authenticated caller. Until that lands there is no attribute that expresses a conjunction —
/// <c>[Roles]</c> is any-of, only the first <c>AuthorizeAttribute</c> is read, and <c>RolesAttribute</c> cannot
/// be applied twice — so this pass keeps the existing behavior rather than substituting a different wrong one.
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
        if (declared.Length == 0 || declared.Any(authorize => authorize?.References().Any() != true))
        {
            return "AllowAnonymous";
        }

        // References() flattens the requirement tree to the policies it names, discarding how they combine —
        // the same flat list the removed Policies property gave, so the rendering below is unchanged. That is
        // the defect in Cratis/Stage#20, not an oversight in absorbing the tree: reading Requirement instead
        // would tell us an 'and' from an 'or', but no Arc attribute can express the difference until
        // Cratis/Arc#2464 makes a named policy actually evaluate. Kept as-is so the fix is one deliberate
        // change against a spec that already documents the wrong answer.
        var required = declared
            .SelectMany(authorize => authorize!.References())
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
