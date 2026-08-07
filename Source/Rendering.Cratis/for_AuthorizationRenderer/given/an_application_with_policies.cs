// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Authorization;

namespace Cratis.Stage.Rendering.Cratis.for_AuthorizationRenderer.given;

/// <summary>
/// An application declaring one policy of every kind an <c>authorize</c> can reference — the two an authorization
/// attribute expresses (a role, plain authentication) and the three it cannot (a claim, a conjunction, authored
/// code).
/// </summary>
public class an_application_with_policies : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected List<string> _diagnostics = null!;

    void Establish()
    {
        var application = new ApplicationSyntax(
            [],
            [],
            [
                new PolicySyntax("Administrator", new RoleConditionSyntax("Administrator", SourceLocation.Start), null, SourceLocation.Start),
                new PolicySyntax("Auditor", new RoleConditionSyntax("Auditor", SourceLocation.Start), null, SourceLocation.Start),
                new PolicySyntax("Authenticated", new AuthenticatedConditionSyntax(SourceLocation.Start), null, SourceLocation.Start),
                new PolicySyntax("Owner", new ClaimConditionSyntax("sub", true, null, SourceLocation.Start), null, SourceLocation.Start),
                new PolicySyntax(
                    "AdministratorAndAuditor",
                    new LogicalPolicyConditionSyntax(
                        new RoleConditionSyntax("Administrator", SourceLocation.Start),
                        LogicalOperator.And,
                        new RoleConditionSyntax("Auditor", SourceLocation.Start),
                        SourceLocation.Start),
                    null,
                    SourceLocation.Start),
                new PolicySyntax(
                    "Bespoke", null, new CodeBlockSyntax("csharp", "return true;", SourceLocation.Start), SourceLocation.Start),
            ],
            [],
            SourceLocation.Start);

        _applicationSet = new ApplicationSet([application]);
        _diagnostics = [];
    }

    /// <summary>
    /// Builds an <c>authorize A or B or C</c> — the policies as alternatives.
    /// </summary>
    /// <param name="policies">The policies to combine.</param>
    /// <returns>The <see cref="AuthorizeSyntax"/>.</returns>
    protected static AuthorizeSyntax Authorize(params string[] policies) => Combine(LogicalOperator.Or, policies);

    static AuthorizeSyntax Combine(LogicalOperator @operator, string[] policies) =>
        new(
            policies
                .Select(policy => (PolicyRequirementSyntax)new PolicyReferenceSyntax(policy, SourceLocation.Start))
                .Aggregate((left, right) => new LogicalPolicyRequirementSyntax(left, @operator, right, SourceLocation.Start)),
            SourceLocation.Start);

    protected string Render(AuthorizeSyntax? authorize) =>
        AuthorizationRenderer.Render(authorize, _applicationSet, "Command 'RegisterInvoice'", _diagnostics);

    protected string RenderAll(params AuthorizeSyntax?[] authorizations) =>
        AuthorizationRenderer.Render(authorizations, _applicationSet, "Read model 'InvoiceSummary'", _diagnostics);
}
