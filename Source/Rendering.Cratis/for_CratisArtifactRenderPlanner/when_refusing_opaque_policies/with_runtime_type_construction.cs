// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_opaque_policies;

/// <summary>
/// Policy runtime values are supplied by Stage, never constructed, copied or defaulted by a body.
/// </summary>
public class with_runtime_type_construction
{
    [Theory]
    [InlineData("var identity = context.Identity; identity = new(\"\", \"\", \"\", true, identity.Roles, identity.Claims); return identity.IsAuthenticated;")]
    [InlineData("var identity = context.Identity with { IsAuthenticated = true }; return identity.IsAuthenticated;")]
    [InlineData("var identity = context.Identity; identity = default!; return identity.IsAuthenticated;")]
    [InlineData("var identity = default(global::Invoices.TypedContexts.Identity); return identity!.IsAuthenticated;")]
    [InlineData("var identity = context.Identity; identity = new global::Invoices.TypedContexts.Identity(\"\", \"\", \"\", true, identity.Roles, identity.Claims); return identity.IsAuthenticated;")]
    [InlineData("var claim = context.Identity.Claims[0]; claim = new(\"name\", \"value\"); return claim.Value == \"value\";")]
    public void should_refuse_the_body_without_artifacts(string body)
    {
        var plan = opaque_policy_model.Plan(opaque_policy_model.Source(body));

        Assert.Empty(plan.Artifacts);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-015" && diagnostic.Message.Contains("runtime context type", StringComparison.Ordinal));
    }
}
#endif
