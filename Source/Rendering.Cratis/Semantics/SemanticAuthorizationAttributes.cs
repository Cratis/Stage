// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Chooses the authorization attributes for admitted commands and queries.
/// </summary>
/// <remarks>
/// An artifact without modeled authorization renders a bare <c language="csharp">[Authorize]</c>: Arc then requires an authenticated
/// caller (with no role or policy requirement). This is Stage's rendered default and deliberately differs from
/// Screenplay's reference semantics, where a missing clause allows everyone. An explicit <c language="play">require anonymous</c>
/// policy instead renders <c language="csharp">[AllowAnonymous]</c>.
/// </remarks>
internal static class SemanticAuthorizationAttributes
{
    const string AuthenticatedCaller = "global::Cratis.Arc.Authorization.AuthorizeAttribute";

    /// <summary>
    /// Gets the diagnostic message for an artifact that has no modeled authorization.
    /// </summary>
    /// <param name="kind">What the artifact is, for example <c language="csharp">Command</c>.</param>
    /// <param name="name">The artifact name.</param>
    /// <returns>The informational message.</returns>
    public static string AuthenticatedByDefault(string kind, string name) =>
        $"{kind} '{name}' requires an authenticated caller because the model has no authorize clause; " +
        "Stage renders an authenticated caller as the default, unlike Screenplay's reference semantics where a missing clause allows everyone.";

    /// <summary>
    /// Gets the authorization attribute for an admitted command.
    /// </summary>
    /// <param name="command">The admitted command.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>The attribute name.</returns>
    public static string For(SemanticCommand command, SemanticApplicationContext context) => command.Authorization is null
        ? AuthenticatedCaller
        : SemanticAnonymousAccess.IsAnonymous(command.Authorization, context.Application.Policies)
            ? "global::Cratis.Arc.Authorization.AllowAnonymousAttribute"
            : $"global::Cratis.Arc.Authorization.AuthorizeAttribute(Policy = {CSharpCodeBuilder.StringLiteral(SemanticPolicyArtifactRenderer.PolicyName(context, command.Id))})";

    /// <summary>
    /// Gets the authorization attribute for an admitted query.
    /// </summary>
    /// <param name="query">The admitted query.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>The attribute name.</returns>
    public static string For(SemanticKeyedQuery query, SemanticApplicationContext context) => query.Authorization is null
        ? AuthenticatedCaller
        : SemanticAnonymousAccess.IsAnonymous(query.Authorization, context.Application.Policies)
            ? "global::Cratis.Arc.Authorization.AllowAnonymousAttribute"
            : $"global::Cratis.Arc.Authorization.AuthorizeAttribute(Policy = {CSharpCodeBuilder.StringLiteral(SemanticPolicyArtifactRenderer.PolicyName(context, query.Id))})";
}
