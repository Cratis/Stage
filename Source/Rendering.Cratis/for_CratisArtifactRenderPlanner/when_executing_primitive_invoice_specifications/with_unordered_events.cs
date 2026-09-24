// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_unordered_events : a_generated_invoice_application
{
    protected override string InvoiceSource => Source;

    internal const string Source = """
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                description String
                streamReference String identifier
                produces InvoiceIssued
                  for streamReference
                  description = description
                produces InvoiceRecorded
                  for streamReference
                  description = description
              event InvoiceIssued
                description String
              event InvoiceRecorded
                description String
              specification IssuingAnInvoice
                when IssueInvoice
                  description = "First payload"
                  streamReference = "invoice-one"
                then events in any order
                then InvoiceRecorded
                  description = "First payload"
                then InvoiceIssued
                  description = "First payload"
        """;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var plan = base.CreatePlan();
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    Task Because() => VerifyGeneratedApplication();

    [Fact] void should_build_debug_without_warnings() => DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_run_the_generated_multiset_specification() => Results.Count(_ => _.Name.Contains("should_append_the_expected_event_multiset", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_run_the_generated_success_and_count_specifications() => Results.Count(_ => _.Name.Contains("should_succeed", StringComparison.Ordinal) || _.Name.Contains("should_append_exactly_2_events", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_pass_every_generated_specification() => (Results.Length > 0 && Results.All(_ => _.Outcome == "Passed")).ShouldBeTrue();
}
#endif
