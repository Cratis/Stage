// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_refusing_opaque_policies;

/// <summary>
/// An authorization receipt-time read needs an authored clock in the generated command scenario.
/// </summary>
public class with_a_specification : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = opaque_policy_model.Source("return context.Occurred.Year == 2027;").Replace(
            "      event InvoiceIssued\n",
            """
                  specification IssuingAsClerk
                    given caller
                      authenticated
                    when IssueInvoice
                      invoiceId = "invoice-one"
                      description = "North"
                    then InvoiceIssued
                      invoiceId = "invoice-one"
                      description = "North"
                  event InvoiceIssued

            """,
            StringComparison.Ordinal);
        _plan = opaque_policy_model.Plan(source);
    }

    [Fact] void should_omit_the_unsupported_specification() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_plan.Refusals.Single().Artifact);
    [Fact] void should_name_the_uncontrolled_receipt_time() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("context.Occurred", StringComparison.Ordinal));
}
#endif
