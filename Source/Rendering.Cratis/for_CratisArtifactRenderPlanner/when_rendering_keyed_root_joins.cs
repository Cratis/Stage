// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_keyed_root_joins : Specification
{
    [Fact]
    public async Task should_emit_the_explicit_key_on_the_root_join()
    {
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(Source));
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        var text = Assert.Single(plan.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())),
            text => text.Contains(".Join<IssueRetitled>(", StringComparison.Ordinal));
        Assert.Contains(".Join<IssueRetitled>(", text, StringComparison.Ordinal);
        Assert.Contains("join.On(model => model.Id)", text, StringComparison.Ordinal);
        Assert.Contains("join.UsingKey(evt => evt.IssueId)", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" key $eventSourceId")]
    public async Task should_keep_default_event_source_joins_without_an_explicit_key(string key)
    {
        var source = Source[..Source.IndexOf("      specification", StringComparison.Ordinal)]
            .Replace(" key issueId", key, StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        var text = Assert.Single(plan.Artifacts.Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan())),
            text => text.Contains(".Join<IssueRetitled>(", StringComparison.Ordinal));
        Assert.DoesNotContain("join.UsingKey(", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task should_keep_literal_keys_refused()
    {
        var source = Source[..Source.IndexOf("      specification", StringComparison.Ordinal)]
            .Replace("from IssueRetitled key issueId", "from IssueRetitled key literal \"constant\"", StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-017");
    }

    [Fact]
    public async Task should_refuse_different_source_backfill_without_blocking_the_projection()
    {
        var source = Source[..Source.IndexOf("      specification", StringComparison.Ordinal)] + """
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
        """;
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" &&
            diagnostic.Message.Contains("https://github.com/Cratis/Chronicle/issues/5439", StringComparison.Ordinal));
        Assert.Contains(plan.Artifacts, artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains(".Join<IssueRetitled>(", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_refuse_a_later_decoy_root_without_blocking_the_projection()
    {
        var source = Source[..Source.IndexOf("      specification", StringComparison.Ordinal)] + """
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
        """;
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" &&
            diagnostic.Message.Contains("https://github.com/Cratis/Chronicle/issues/5439", StringComparison.Ordinal));
        Assert.Contains(plan.Artifacts, artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains(".Join<IssueRetitled>(", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_a_later_given_root_or_command(bool command)
    {
        const string action = """
                given IssueFiled
                  for "ISSUE-1"
                  title = "first"
                when append IssueRetitled
                  for "ISSUE-2"
                  issueId = "ISSUE-1"
                  title = "retitled"
        """;
        var source = Source[..Source.IndexOf("      specification", StringComparison.Ordinal)] + $$"""
              specification later_root
                given IssueRetitled
                  for "ISSUE-2"
                  issueId = "ISSUE-1"
                  title = "retitled"
        {{action}}
                then readmodel Filed
                  id = "ISSUE-1"
                  title = "retitled"
        """;
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        if (command)
        {
            var specification = loaded.Plan.Specifications.Values.Single();
            var commanded = specification with { GivenEvents = [specification.GivenEvents[0]], WhenAppended = null, When = new(loaded.Plan.Commands.Values.First().Id, []) };
            Assert.Contains("https://github.com/Cratis/Chronicle/issues/5439", SemanticKeyedRootJoinOrdering.Divergence(loaded.Plan.Projections.Values, commanded), StringComparison.Ordinal);
            return;
        }
        var plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011" &&
            diagnostic.Message.Contains("https://github.com/Cratis/Chronicle/issues/5439", StringComparison.Ordinal));
    }

    internal const string Source = """
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
        """;
}
#endif
