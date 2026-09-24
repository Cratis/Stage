// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_explicitly_sourced_given_events : an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "when IssueInvoice\n          description = \"First payload\"",
        "given InvoiceIssued\n          for \"prior-one\"\n          description = \"Prior first\"\n        given InvoiceIssued\n          for \"prior-two\"\n          description = \"Prior second\"\n        when IssueInvoice\n          description = \"First payload\"",
        StringComparison.Ordinal));

    [Fact] void should_admit_sourced_prior_facts() => _plan.Success.ShouldBeTrue();
    [Fact] void should_seed_both_prior_facts_on_their_stated_streams() => Artifact("when_issuing_first_invoice.cs").ShouldContain("ForEventSource(\"prior-two\").Events(new InvoiceIssued(\"Prior second\"))");
    [Fact] void should_seed_prior_facts_before_executing_the_command() =>
        Artifact("when_issuing_first_invoice.cs").IndexOf("Prior first", StringComparison.Ordinal)
            .ShouldBeLessThan(Artifact("when_issuing_first_invoice.cs").IndexOf("async Task Because", StringComparison.Ordinal));
}
