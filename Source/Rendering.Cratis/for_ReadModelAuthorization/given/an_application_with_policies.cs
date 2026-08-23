// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Authorization;

namespace Cratis.Stage.Rendering.Cratis.for_ReadModelAuthorization.given;

/// <summary>
/// Helpers for hanging query declarations off the read models a slice declares and observing whether only the
/// synthesized fixed pair receives type-level authorization.
/// </summary>
public class an_application_with_policies : Specification
{
    protected List<string> _diagnostics = null!;

    void Establish() => _diagnostics = [];

    /// <summary>
    /// Builds a query answering with one instance of a read model, guarded by the named policies — or by nothing
    /// at all when none are named.
    /// </summary>
    /// <param name="name">The name of the query.</param>
    /// <param name="readModel">The read model the query reads, which its return type names.</param>
    /// <param name="policies">The policies any one of which may read it; none leaves the query unguarded.</param>
    /// <returns>The <see cref="QuerySyntax"/>.</returns>
    protected static QuerySyntax Query(string name, string readModel, params string[] policies) =>
        Build(name, readModel, isCollection: false, policies);

    /// <summary>
    /// Builds a query answering with a collection of a read model, which reads it just as a single instance does.
    /// </summary>
    /// <param name="name">The name of the query.</param>
    /// <param name="readModel">The read model the query reads, which its return type names.</param>
    /// <param name="policies">The policies any one of which may read it; none leaves the query unguarded.</param>
    /// <returns>The <see cref="QuerySyntax"/>.</returns>
    protected static QuerySyntax QueryForMany(string name, string readModel, params string[] policies) =>
        Build(name, readModel, isCollection: true, policies);

    static QuerySyntax Build(string name, string readModel, bool isCollection, string[] policies) =>
        new(
            name,
            new TypeRefSyntax(readModel, isCollection, false, SourceLocation.Start),
            null,
            [],
            Authorize(policies),
            SourceLocation.Start);

    static AuthorizeSyntax? Authorize(string[] policies) =>
        policies.Length == 0
            ? null
            : new AuthorizeSyntax(
                policies
                    .Select(policy => (PolicyRequirementSyntax)new PolicyReferenceSyntax(policy, SourceLocation.Start))
                    .Aggregate((left, right) => new LogicalPolicyRequirementSyntax(left, LogicalOperator.Or, right, SourceLocation.Start)),
                SourceLocation.Start);

    protected string? Render(string readModel, params QuerySyntax[] queries) =>
        ReadModelAuthorization.Render(readModel, queries, _diagnostics);
}
