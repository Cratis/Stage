// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Claims;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_opaque_policies;

/// <summary>
/// Screenplay keeps roles and claims separate: a claim of the principal's role claim type is a role, not a claim.
/// </summary>
public class with_role_claims
{
    [Theory]
    [InlineData("return context.Identity.HasRole(\"Clerk\");", true)]
    [InlineData("foreach (var role in context.Identity.Roles)\n{\n  if (role == \"Clerk\") return true;\n}\nreturn false;", true)]
    [InlineData("return context.Identity.HasClaim(\"" + ClaimTypes.Role + "\");", false)]
    [InlineData("return context.Identity.ClaimValue(\"" + ClaimTypes.Role + "\") is not null;", false)]
    [InlineData("return context.Identity.ClaimValues(\"" + ClaimTypes.Role + "\").Length > 0;", false)]
    [InlineData("foreach (var claim in context.Identity.Claims)\n{\n  if (claim.Value == \"Clerk\") return true;\n}\nreturn false;", false)]
    [InlineData("return context.Identity.HasClaim(\"department\");", true)]
    public void should_expose_a_role_claim_only_as_a_role(string body, bool allowed)
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source(body));
        var plan = opaque_policy_model.Plan(loaded);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, "Clerk"), new Claim("department", "Sales")], "fixture"));
        opaque_policy_model.AllowsCommand(opaque_policy_model.Compile(plan), loaded.Model, principal, opaque_policy_model.Receipt).ShouldEqual(allowed);
    }
}
#endif
