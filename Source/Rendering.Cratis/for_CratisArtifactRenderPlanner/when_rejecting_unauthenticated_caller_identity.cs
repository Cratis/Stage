// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_unauthenticated_caller_identity
{
    [Theory]
    [InlineData(false, "role \"Staff\"")]
    [InlineData(false, "claim \"department\" = \"Sales\"")]
    [InlineData(true, "role \"Staff\"")]
    [InlineData(true, "claim \"department\" = \"Sales\"")]
    public void should_omit_the_unrepresentable_fixture(bool queryOnly, string identity)
    {
        var action = queryOnly
            ? "then query InvoiceById\n          arguments\n            invoiceId = \"invoice-one\""
            : "when IssueInvoice\n          invoiceId = \"invoice-one\"\n          description = \"North\"";
        var specification = $"\n      specification ImpossibleGuest\n        given caller\n          {identity}\n        {action}\n        then denied\n";
        var source = queryOnly
            ? when_rendering_portable_authorization.Source + specification
            : when_rendering_portable_authorization.Source.Replace("    slice StateView Lookup", specification + "    slice StateView Lookup", StringComparison.Ordinal);
        var model = invoice_model.Compile(source);
        var fixture = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications).Single();
        var plan = invoice_model.Plan(model);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Artifact == fixture.Id &&
            diagnostic.Message.Contains("An unauthenticated caller cannot carry roles or claims; Arc supplies an empty guest principal.", StringComparison.Ordinal));
        Assert.True(plan.Success);
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.Sources.Contains(fixture.Id));
    }
}
