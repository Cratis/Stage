// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_portable_constraints.with_a_unique_event_occurrence.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_portable_constraints;

public class with_a_unique_event_occurrence(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_pass_generated_specs() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_execute_violation_and_nonviolating_cases() => fixture.Results.Length.ShouldEqual(9);

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace(
                "      event InvoiceIssued\n",
                """
                  constraint OneInvoicePerStream
                    unique event InvoiceIssued
                  event InvoiceIssued
            """ + "\n",
                StringComparison.Ordinal)
            .Replace(
                "      specification IssuingSecondInvoice\n",
                """
                  specification RepeatingInvoice
                    given InvoiceIssued
                      for "invoice-one"
                      description = "First payload"
                    when IssueInvoice
                      description = "First payload"
                      streamReference = "invoice-one"
                    then error "Constraint 'OneInvoicePerStream' is violated: the event source already has the constrained event."
                  specification IssuingSecondInvoice
            """ + "\n",
                StringComparison.Ordinal);

        Task Because() => VerifyGeneratedApplication();
    }
}
#endif
