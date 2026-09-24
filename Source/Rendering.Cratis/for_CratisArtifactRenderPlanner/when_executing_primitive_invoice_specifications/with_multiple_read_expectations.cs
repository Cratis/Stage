// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications.with_multiple_read_expectations.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_primitive_invoice_specifications;

public class with_multiple_read_expectations(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldBeEmpty();
    [Fact] void should_run_all_generated_assertions() => fixture.Results.Length.ShouldEqual(8);
    [Fact] void should_pass_every_assertion() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => """
            concept InvoiceId : String
            module Billing
              feature Invoicing
                slice StateChange Issue
                  command IssueInvoice
                    streamReference InvoiceId identifier
                    description String
                    produces InvoiceIssued
                      for streamReference
                      streamReference = streamReference
                      description = description
                  event InvoiceIssued
                    streamReference InvoiceId
                    description String
                  specification IssuingAnInvoice
                    when IssueInvoice
                      streamReference = "invoice-one"
                      description = "A payload"
                    then InvoiceIssued
                      streamReference = "invoice-one"
                      description = "A payload"
                    then readmodel InvoiceSummary
                      streamReference = "invoice-one"
                      description = "A payload"
                    then readmodel InvoiceDetails
                      streamReference = "invoice-one"
                      description = "A payload"
                    then query InvoiceById
                      arguments
                        streamReference = "invoice-one"
                      result
                        streamReference = "invoice-one"
                        description = "A payload"
                    then query InvoiceDetailsById
                      arguments
                        streamReference = "invoice-one"
                      result
                        streamReference = "invoice-one"
                        description = "A payload"
                slice StateView Summary
                  readmodel InvoiceSummary
                    streamReference InvoiceId
                    description String
                  query InvoiceById => InvoiceSummary?
                    by streamReference InvoiceId
                  projection InvoiceSummaryProjection => InvoiceSummary
                    from InvoiceIssued key streamReference
                      streamReference = streamReference
                      description = description
                slice StateView Details
                  readmodel InvoiceDetails
                    streamReference InvoiceId
                    description String
                  query InvoiceDetailsById => InvoiceDetails?
                    by streamReference InvoiceId
                  projection InvoiceDetailsProjection => InvoiceDetails
                    from InvoiceIssued key streamReference
                      streamReference = streamReference
                      description = description
            """;

        protected override ArtifactRenderPlan CreatePlan()
        {
            var plan = base.CreatePlan();
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics.Select(_ => $"{_.Code}: {_.Message}")));
            return plan;
        }

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
