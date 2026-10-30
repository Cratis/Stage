// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_lowering_anonymous_operations : Specification
{
    [Theory]
    [InlineData("")]
    [InlineData("id String identifier")]
    [InlineData("id RecordId generated identifier")]
    public void should_not_require_an_authorization_subject(string identifier)
    {
        var model = invoice_model.Compile($"""
            concept RecordId : Uuid
            policy Public
              require anonymous
            module M
              feature F
                slice StateChange S
                  command Record
                    {identifier}
                    name String
                    authorize Public
                    produces Recorded
                      for name
                      name = name
                  event Recorded
                    name String
            """);
        var plan = invoice_model.Plan(model);
        Assert.True(plan.Success, string.Join(" | ", plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var source = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/S.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        source.ShouldContain("[AllowAnonymous]");
        plan.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-060").ShouldEqual(1);
        plan.Artifacts.ShouldNotContain(artifact => artifact.RelativePath.Contains("PublicPolicy", StringComparison.Ordinal));
    }

    [Fact]
    public void should_allow_an_anonymous_live_composite_key_collection()
    {
        var source = when_refusing_composite_key_shapes.Valid.Replace("module Billing", "policy Public\n  require anonymous\nmodule Billing", StringComparison.Ordinal)
            .Replace("      projection PaymentTotals", "      query LiveTotals => observable PaymentTotals[]\n        by invoiceId InvoiceId\n        authorize Public\n      projection PaymentTotals", StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.True(plan.Success, string.Join(" | ", plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
        plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-048");
        plan.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-ESM-060").ShouldEqual(1);
        var query = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Totals.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        query.ShouldContain("[AllowAnonymous]");
    }
}
