// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_opaque_policies;

/// <summary>
/// Opaque and portable terms compose left to right. An opaque term reached without a receipt time denies the whole
/// authorization; one that is never reached cannot deny it.
/// </summary>
public class with_portable_composition
{
    const string Body = "return context.Identity.HasRole(\"Clerk\");";

    [Theory]
    [InlineData("Staff or Custom", new[] { "Staff" }, false, true)]
    [InlineData("Staff or Custom", new string[0], false, false)]
    [InlineData("Staff or Custom", new[] { "Clerk" }, true, true)]
    [InlineData("Custom or Staff", new[] { "Staff" }, false, false)]
    [InlineData("Custom or Staff", new[] { "Staff" }, true, true)]
    [InlineData("Custom and Staff", new[] { "Clerk" }, true, false)]
    [InlineData("Custom and Staff", new[] { "Clerk", "Staff" }, true, true)]
    [InlineData("Staff and Custom", new[] { "Clerk" }, false, false)]
    [InlineData("Staff and Custom", new[] { "Staff", "Clerk" }, false, false)]
    public void should_evaluate_terms_in_authored_order(string authorization, string[] roles, bool received, bool allowed)
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source(Body, authorization, authorization));
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var assembly = opaque_policy_model.Compile(plan);
        var receivedAt = received ? opaque_policy_model.Receipt : default;
        opaque_policy_model.AllowsCommand(assembly, loaded.Model, opaque_policy_model.Caller(roles), receivedAt).ShouldEqual(allowed);
        opaque_policy_model.AllowsQuery(assembly, loaded.Model, opaque_policy_model.Caller(roles), receivedAt, "invoice-one").ShouldEqual(allowed);
    }
}
#endif
