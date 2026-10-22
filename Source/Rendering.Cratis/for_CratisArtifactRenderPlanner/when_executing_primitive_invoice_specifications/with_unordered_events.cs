// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_unordered_events(with_unordered_events.context fixture) : IClassFixture<with_unordered_events.context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_run_the_generated_multiset_specification() => fixture.Results.Count(_ => _.Name.Contains("should_append_the_expected_event_multiset", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_run_the_generated_success_and_count_specifications() => fixture.Results.Count(_ => _.Name.Contains("should_succeed", StringComparison.Ordinal) || _.Name.Contains("should_append_exactly_2_events", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_pass_every_generated_specification() => (fixture.Results.Length > 0 && fixture.Results.All(_ => _.Outcome == "Passed")).ShouldBeTrue();

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

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => Source;

        protected override ArtifactRenderPlan CreatePlan()
        {
            var plan = base.CreatePlan();
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
            return plan;
        }

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
