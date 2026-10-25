// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rejecting_an_optional_event_property : when_planning_newer_semantic_constructs.given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace("event InvoiceIssued\n        description String", "event InvoiceIssued\n        description String?", StringComparison.Ordinal));

    [Fact] void should_keep_the_refusal_code() => RefusalCodes.ShouldContainOnly(["STAGE-ESM-005"]);
    [Fact] void should_name_the_event_and_property_and_both_fix_parts() => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-005" && !diagnostic.Message.StartsWith("Generated artifact '", StringComparison.Ordinal)).Message.ShouldEqual("Command 'IssueInvoice' event 'InvoiceIssued' property 'description' is optional; Cratis events have no optional properties. Make it required (in Studio's model board, tick 'Required' on the property) or remove it.");
    [Fact] void should_omit_the_refused_parts_and_compile_the_rest() => AssertOmittedParts();
}
