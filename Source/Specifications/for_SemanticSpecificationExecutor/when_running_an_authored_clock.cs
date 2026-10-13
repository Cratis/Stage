// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_an_authored_clock : Specification
{
    const string Source = """
        concept InvoiceId : String
        module Billing
          feature Invoicing
            slice StateChange Issue
              command IssueInvoice
                invoiceId InvoiceId identifier
                produces event InvoiceIssued
                  issuedAt DateTime = $context.occurred
              specification AtTheAuthoredInstant
                given clock "2027-03-04T12:00:00.123+02:00"
                when IssueInvoice
                  invoiceId = "invoice-one"
                then InvoiceIssued
                  issuedAt = "2027-03-04T10:00:00.123Z"
        """;

    readonly List<SemanticSpecificationRunRecord> _results = [];
    readonly List<bool> _reference = [];

    async Task Because()
    {
        foreach (var source in new[] { Source, Source.Replace("issuedAt = \"2027-03-04T10:00:00.123Z\"", "issuedAt = \"2027-03-04T10:00:00.124Z\"", StringComparison.Ordinal) })
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ClockApp"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("clock"), "clock", "clock.play", source);
            var compilation = new SemanticModelCompiler().Compile("ClockApp", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics.Select(value => value.Message)));
            var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
            var report = await new SemanticSpecificationExecutor().Run(plan, SemanticSpecificationSelection.All, new() { Clock = new UnexpectedClock() });
            _results.Add(report.Results.Single());
            _reference.Add(new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single()).Passed);
        }
    }

    [Fact] void should_use_the_authored_clock_instead_of_the_options_clock() => _results[0].Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_fail_for_a_wrong_expected_instant() => _results[1].Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_agree_with_the_reference_for_a_correct_instant() => _reference[0].ShouldBeTrue();
    [Fact] void should_agree_with_the_reference_for_a_wrong_instant() => _reference[1].ShouldBeFalse();

    sealed class UnexpectedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => throw new OptionsClockRead();
    }

    sealed class OptionsClockRead() : Exception("An authored specification clock must override the options clock.");
}
