// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_multipart_projection_lifecycle : Specification
{
    SemanticSpecificationRunReport _report = null!;
    int _missingPartResults;

    async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("MultipartLifecycle"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "application.play", Source);
        var compilation = new SemanticModelCompiler().Compile("MultipartLifecycle", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _report = await new SemanticSpecificationExecutor().Run(plan, new([]), new());
        var assertion = plan.Specifications.Values.Single(specification => specification.Name == "Found").ThenQueries.Single();
        var key = (SemanticCompositeValue)assertion.Key;
        var row = new Comparison.SemanticRunProjections.Projected(plan.ReadModels.Values.Single().Id, key, assertion.Results.Single().Values);
        _missingPartResults = Comparison.SemanticRunQueries.Select(plan, assertion with { Key = key with { Properties = [key.Properties[0]] } }, [row]).Length;
    }

    [Fact] void should_run_every_case() => _report.Results.Count.ShouldEqual(6);
    [Fact] void should_update_only_the_complete_key() => Assert.True(Result("Updated").Outcome == SemanticSpecificationOutcome.Passed, string.Join(';', Result("Updated").Failures));
    [Fact] void should_lookup_by_all_parts_independent_of_argument_order() => Assert.True(Result("Found").Outcome == SemanticSpecificationOutcome.Passed, string.Join(';', Result("Found").Failures));
    [Fact] void should_not_match_a_missing_complete_key() => Result("Missing").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_remove_only_the_complete_key() => Result("Removed").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_fail_a_wrong_value() => Result("WrongValue").Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_fail_a_wrong_absence() => Result("WrongAbsence").Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_not_report_unsupported() => Assert.All(_report.Results, result => result.Unsupported.ShouldBeNull());
    [Fact] void should_not_treat_an_omitted_query_part_as_a_wildcard() => _missingPartResults.ShouldEqual(0);
    [Fact] void should_trace_each_distinct_updated_key() => Result("Updated").Trace!.ReadModels.Count.ShouldEqual(3);
    [Fact] void should_report_a_value_mismatch_not_an_execution_exception() => Result("WrongValue").Failures.Single().ShouldContain("was not found with matching values.");
    [Fact] void should_report_an_absence_mismatch_not_an_execution_exception() => Result("WrongAbsence").Failures.Single().ShouldContain("to be absent.");

    SemanticSpecificationRunRecord Result(string name) => _report.Results.Single(result => result.Name == name);

    const string Source = """
        module Billing
          feature Payments
            slice StateView Status
              event Paid
                tenant String
                invoice String
                amount Int
              event Cancelled
                tenant String
                invoice String
              readmodel Payment
                tenant String key
                invoice String key
                amount Int
              projection Payments => Payment
                from Paid
                  key
                    invoice = invoice
                    tenant = tenant
                  amount = amount
                remove with Cancelled
                  key
                    tenant = tenant
                    invoice = invoice
              query ByKey => Payment optional
                by
                  tenant String
                  invoice String
              specification Updated
                given Paid
                  tenant = "north|a"
                  invoice = "b"
                  amount = 1
                given Paid
                  tenant = "north"
                  invoice = "a|b"
                  amount = 2
                given Paid
                  tenant = "north|a"
                  invoice = "c"
                  amount = 3
                when append Paid
                  tenant = "north|a"
                  invoice = "b"
                  amount = 9
                then readmodel Payment
                  tenant = "north|a"
                  invoice = "b"
                  amount = 9
                then readmodel Payment
                  tenant = "north"
                  invoice = "a|b"
                  amount = 2
                then readmodel Payment
                  tenant = "north|a"
                  invoice = "c"
                  amount = 3
              specification Found
                given Paid
                  tenant = "north"
                  invoice = "a"
                  amount = 1
                given Paid
                  tenant = "north"
                  invoice = "b"
                  amount = 2
                then query ByKey
                  arguments
                    invoice = "b"
                    tenant = "north"
                  result
                    tenant = "north"
                    invoice = "b"
                    amount = 2
              specification Missing
                given Paid
                  tenant = "north"
                  invoice = "a"
                  amount = 1
                then query ByKey
                  arguments
                    tenant = "other"
                    invoice = "a"
              specification Removed
                given Paid
                  tenant = "north"
                  invoice = "a"
                  amount = 1
                given Paid
                  tenant = "north"
                  invoice = "b"
                  amount = 2
                when append Cancelled
                  tenant = "north"
                  invoice = "a"
                then no readmodel Payment for {"invoice":"a","tenant":"north"}
                then readmodel Payment
                  tenant = "north"
                  invoice = "b"
                  amount = 2
              specification WrongValue
                when append Paid
                  tenant = "north"
                  invoice = "a"
                  amount = 1
                then readmodel Payment
                  tenant = "north"
                  invoice = "a"
                  amount = 2
              specification WrongAbsence
                when append Paid
                  tenant = "north"
                  invoice = "a"
                  amount = 1
                then no readmodel Payment for {"tenant":"north","invoice":"a"}
        """;
}
