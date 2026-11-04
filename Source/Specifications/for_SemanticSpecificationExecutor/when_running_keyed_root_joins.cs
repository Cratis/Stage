// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_keyed_root_joins : Specification
{
    SemanticExecutionPlan _plan = null!;

    void Establish() => _plan = when_running_property_keyed_removal_only_projections.Compile(Source);

    [Fact]
    public async Task should_update_the_keyed_issue_and_leave_the_event_source_issue_unchanged()
    {
        var specification = Named("retitling_the_keyed_issue");
        SemanticRunAdmission.Check(_plan, specification).ShouldBeNull();
        new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed.ShouldBeTrue();
        var report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());
        var result = report.Results.Single(result => result.Name == specification.Name);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        var model = _plan.ReadModels.Values.Single();
        var title = model.Properties.Single(property => property.Name == "title").Id.ToString();
        result.Trace.ReadModels.Count.ShouldEqual(2);
        AssertTitle("ISSUE-1", "retitled");
        AssertTitle("ISSUE-2", "second");

        void AssertTitle(string key, string expected)
        {
            using var row = JsonDocument.Parse(result.Trace.ReadModels[$"{model.Id}:{JsonSerializer.Serialize(key)}"]);
            row.RootElement.GetProperty(title).GetString().ShouldEqual(JsonSerializer.Serialize(expected));
        }
    }

    [Fact]
    public void should_refuse_a_different_source_join_before_its_root()
    {
        var specification = Named("retitling_before_filing_with_a_different_source");
        new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed.ShouldBeTrue();
        var reason = SemanticRunAdmission.Check(_plan, specification);
        Assert.NotNull(reason);
        Assert.Contains("https://github.com/Cratis/Chronicle/issues/5439", reason.Details, StringComparison.Ordinal);
    }

    [Fact]
    public void should_refuse_a_later_decoy_root_after_a_different_source_join()
    {
        var specification = Named("updating_the_decoy_after_retitling");
        new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed.ShouldBeTrue();
        var reason = SemanticRunAdmission.Check(_plan, specification);
        Assert.NotNull(reason);
        Assert.Contains("https://github.com/Cratis/Chronicle/issues/5439", reason.Details, StringComparison.Ordinal);
    }

    [Fact]
    public async Task should_admit_backfill_when_the_key_matches_the_event_source()
    {
        var specification = Named("retitling_before_filing_with_the_same_source");
        SemanticRunAdmission.Check(_plan, specification).ShouldBeNull();
        new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed.ShouldBeTrue();
        var report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());
        var result = report.Results.Single(result => result.Name == specification.Name);
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Unsupported?.Details} {string.Join(';', result.Failures)}");
    }

    SemanticSpecification Named(string name) => _plan.Specifications.Values.Single(specification => specification.Name == name);

    const string Source = """
        module Issues
          feature Tracking
            slice StateChange FileIssue
              command FileIssue
                id String identifier
                title String
                produces IssueFiled
                  for id
                  title = title
              event IssueFiled
                title String
            slice StateChange RetitleIssue
              command RetitleIssue
                sourceId String identifier
                issueId String
                title String
                produces IssueRetitled
                  for sourceId
                  issueId = issueId
                  title = title
              event IssueRetitled
                issueId String
                title String
            slice StateView IssueList
              readmodel Filed
                id String key
                title String
              projection WorkItem
                from IssueRetitled key issueId
                  title = title
                variant Filed
                  enters on IssueFiled
              specification retitling_the_keyed_issue
                given IssueFiled
                  for "ISSUE-1"
                  title = "first"
                given IssueFiled
                  for "ISSUE-2"
                  title = "second"
                when append IssueRetitled
                  for "ISSUE-2"
                  issueId = "ISSUE-1"
                  title = "retitled"
                then readmodel Filed
                  id = "ISSUE-1"
                  title = "retitled"
                then readmodel Filed
                  id = "ISSUE-2"
                  title = "second"
              specification retitling_before_filing_with_a_different_source
                given IssueRetitled
                  for "ISSUE-2"
                  issueId = "ISSUE-1"
                  title = "retitled"
                when append IssueFiled
                  for "ISSUE-1"
                  title = "first"
                then readmodel Filed
                  id = "ISSUE-1"
                  title = "retitled"
              specification updating_the_decoy_after_retitling
                given IssueFiled
                  for "ISSUE-1"
                  title = "first"
                given IssueRetitled
                  for "ISSUE-2"
                  issueId = "ISSUE-1"
                  title = "retitled"
                when append IssueFiled
                  for "ISSUE-2"
                  title = "second"
                then readmodel Filed
                  id = "ISSUE-1"
                  title = "retitled"
                then readmodel Filed
                  id = "ISSUE-2"
                  title = "second"
              specification retitling_before_filing_with_the_same_source
                given IssueRetitled
                  for "ISSUE-1"
                  issueId = "ISSUE-1"
                  title = "retitled"
                when append IssueFiled
                  for "ISSUE-1"
                  title = "first"
                then readmodel Filed
                  id = "ISSUE-1"
                  title = "retitled"
        """;
}
