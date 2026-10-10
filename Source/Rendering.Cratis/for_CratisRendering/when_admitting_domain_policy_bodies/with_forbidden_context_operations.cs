// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_admitting_domain_policy_bodies;

public class with_forbidden_context_operations
{
    [Theory]
    [InlineData("return context.Tenant is not null;")]
    [InlineData("return context.Artifact is not null;")]
    [InlineData("return context.Identity.Id == \"id\";")]
    [InlineData("return context.Identity.Name == \"Ada\";")]
    [InlineData("return context.Identity.UserName == \"ada\";")]
    [InlineData("var identity = context.Identity; identity = new(\"\", \"\", \"\", true, identity.Roles, identity.Claims); return identity.IsAuthenticated;")]
    [InlineData("var identity = context.Identity with { IsAuthenticated = true }; return identity.IsAuthenticated;")]
    [InlineData("var identity = context.Identity; identity = default!; return identity.IsAuthenticated;")]
    [InlineData("var identity = default(global::Invoices.TypedContexts.Identity); return identity!.IsAuthenticated;")]
    [InlineData("var claim = context.Identity.Claims[0]; claim = new(\"name\", \"value\"); return claim.Value == \"value\";")]
    [InlineData("var copy = context; copy = new(null!, \"\", context.Identity, null!, context.Occurred); return copy.Subject == \"\";")]
    [InlineData("var copy = context with { Subject = \"forged\" }; return copy.Subject == \"forged\";")]
    [InlineData("var copy = context; copy = default!; return copy.Subject == \"\";")]
    public void should_refuse_the_body_without_artifacts(string body)
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source(body));
        var plan = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Module("Billing")]), new("InvoiceModel", "InvoiceApp", "Invoices") { Domain = "Sales/Retail" });

        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-015");
    }
}
#endif
