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
/// A role policy, or an explicit disjunction made entirely from role policies, has the same any-of semantics as
/// Arc's <c>[Roles]</c> attribute and is preserved exactly. Authentication alone renders as <c>[Authorize]</c>.
/// No <c>authorize</c> declaration renders as <c>[AllowAnonymous]</c>.
/// </para>
/// <para>
/// Conjunctions and requirements involving claims, authored code, missing policies, or unknown policy syntax do
/// not have a faithful Arc attribute equivalent. These outcomes throw <see cref="AuthorizationCannotBeRendered"/>
/// so the containing artifact is not emitted. In particular, <c>authorize A B</c> is a conjunction and is never
/// weakened into the disjunction <c>[Roles("A", "B")]</c>. Faithful rendering of those requirements depends on
/// the future Screenplay-owned portable policy backend.
/// </para>
/// </remarks>
public static class AuthorizationRenderer
{
    /// <summary>
    /// The namespace the rendered authorization attributes live in.
    /// </summary>
    public const string Namespace = "Cratis.Arc.Authorization";

    /// <summary>
    /// The stable diagnostic code reported when authorization requires the portable policy backend to render
    /// faithfully.
    /// </summary>
    public const string PortablePolicyRequiredDiagnosticCode = AuthorizationCannotBeRendered.DiagnosticCode;

    /// <summary>
    /// Renders the authorization attribute for a single <c>authorize</c> declaration.
    /// </summary>
    /// <param name="authorize">The declaration, or <see langword="null"/> when the construct declares none.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> the policies are resolved against.</param>
    /// <param name="subject">What is being authorized, for diagnostics (for example <c>Command 'Invite'</c>).</param>
    /// <param name="diagnostics">Collects anything that could not be rendered faithfully.</param>
    /// <returns>The attribute content, without the surrounding brackets.</returns>
    /// <exception cref="AuthorizationCannotBeRendered">The authorization cannot be represented faithfully.</exception>
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
    /// <exception cref="AuthorizationCannotBeRendered">The authorization cannot be represented faithfully.</exception>
    /// <exception cref="UnreachableAuthorizationRendering">An internal authorization outcome is not handled.</exception>
    public static string Render(
        IEnumerable<AuthorizeSyntax?> authorizations, ApplicationSet applicationSet, string subject, ICollection<string> diagnostics)
    {
        var outcome = Resolve(authorizations, applicationSet);
        if (outcome is UnsupportedAuthorization unsupported)
        {
            var exception = new AuthorizationCannotBeRendered(subject, unsupported.Reason);
            diagnostics.Add(exception.Message);
            throw exception;
        }

        return outcome switch
        {
            AnonymousAccess => "AllowAnonymous",
            AuthenticatedOnly => "Authorize",
            RoleDisjunction roles =>
                $"Roles({string.Join(", ", roles.Roles.Select(CSharpCodeBuilder.StringLiteral))})",
            _ => throw new UnreachableAuthorizationRendering(outcome.GetType()),
        };
    }

    static AuthorizationRendering Resolve(IEnumerable<AuthorizeSyntax?> authorizations, ApplicationSet applicationSet)
    {
        var declared = authorizations.ToArray();
        if (declared.Length == 0)
        {
            return AnonymousAccess.Instance;
        }

        return Alternative(declared.Select(authorize =>
            authorize is null ? AnonymousAccess.Instance : Resolve(authorize.Requirement, applicationSet)));
    }

    static AuthorizationRendering Resolve(PolicyRequirementSyntax requirement, ApplicationSet applicationSet) =>
        requirement switch
        {
            PolicyReferenceSyntax reference => Resolve(reference, applicationSet),
            LogicalPolicyRequirementSyntax { Operator: LogicalOperator.Or } logical => Alternative(
                Resolve(logical.Left, applicationSet),
                Resolve(logical.Right, applicationSet)),
            LogicalPolicyRequirementSyntax { Operator: LogicalOperator.And } => new UnsupportedAuthorization(
                "declares a conjunction, which Arc's role attribute would weaken to a disjunction"),
            _ => new UnsupportedAuthorization("contains a policy requirement this renderer does not understand"),
        };

    static AuthorizationRendering Resolve(PolicyReferenceSyntax reference, ApplicationSet applicationSet)
    {
        if (!applicationSet.Policies.TryGetValue(reference.Name, out var policy))
        {
            return new UnsupportedAuthorization($"references policy '{reference.Name}', which nothing declares");
        }

        if (policy.Code is not null)
        {
            return new UnsupportedAuthorization(
                $"references policy '{policy.Name}', whose requirement is an authored {policy.Code.Language} block");
        }

        return Resolve(policy.Name, policy.Condition);
    }

    static AuthorizationRendering Resolve(string policy, PolicyConditionSyntax? condition) => condition switch
    {
        AuthenticatedConditionSyntax => AuthenticatedOnly.Instance,
        RoleConditionSyntax role => new RoleDisjunction([role.Role]),
        LogicalPolicyConditionSyntax { Operator: LogicalOperator.Or } logical => Alternative(
            Resolve(policy, logical.Left),
            Resolve(policy, logical.Right)),
        _ => new UnsupportedAuthorization($"references policy '{policy}', which requires {Describe(condition)}"),
    };

    static AuthorizationRendering Alternative(params AuthorizationRendering[] alternatives) => Alternative(alternatives.AsEnumerable());

    static AuthorizationRendering Alternative(IEnumerable<AuthorizationRendering> alternatives)
    {
        var outcomes = alternatives.ToArray();
        var unsupported = outcomes.OfType<UnsupportedAuthorization>().FirstOrDefault();
        if (unsupported is not null)
        {
            return unsupported;
        }

        if (outcomes.Any(outcome => outcome is AnonymousAccess))
        {
            return AnonymousAccess.Instance;
        }

        if (outcomes.Any(outcome => outcome is AuthenticatedOnly))
        {
            return AuthenticatedOnly.Instance;
        }

        return new RoleDisjunction(
            [.. outcomes.OfType<RoleDisjunction>().SelectMany(outcome => outcome.Roles).Distinct(StringComparer.Ordinal)]);
    }

    static string Describe(PolicyConditionSyntax? condition) => condition switch
    {
        null => "nothing",
        ClaimConditionSyntax claim => $"the claim '{claim.Claim}'",
        LogicalPolicyConditionSyntax { Operator: LogicalOperator.And } => "more than one condition at once",
        _ => "a condition this renderer does not understand",
    };

    abstract record AuthorizationRendering;

    sealed record AnonymousAccess : AuthorizationRendering
    {
        public static readonly AnonymousAccess Instance = new();
    }

    sealed record AuthenticatedOnly : AuthorizationRendering
    {
        public static readonly AuthenticatedOnly Instance = new();
    }

    sealed record RoleDisjunction(string[] Roles) : AuthorizationRendering;

    sealed record UnsupportedAuthorization(string Reason) : AuthorizationRendering;
}
