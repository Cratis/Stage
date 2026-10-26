// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_analyzing_a_reducer_with_a_keyed_query;

public class with_a_body
{
    const string IssueBody = "return new ChangeRequest(context.Key, context.Event.Reason, context.Event.RequestedAt);";
    const string ConstructedBody = "return new ChangeRequest(TalkId: new TalkId(context.Key), Reason: context.Event.Reason, RequestedAt: context.Event.RequestedAt);";
    const string Source = """
        concept TalkId : String
        concept Reason : String
        module Talks
          feature Changes
            slice StateChange RequestChanges
              command RequestChanges
                talkId TalkId identifier
                reason Reason
                requestedAt DateTime
                produces ChangesRequested
                  for talkId
                  reason = reason
                  requestedAt = requestedAt
              event ChangesRequested
                reason Reason
                requestedAt DateTime
            slice StateView ChangeRequests
              readmodel ChangeRequest
                talkId TalkId key
                reason Reason
                requestedAt DateTime
              reducer OpenChangeRequests => ChangeRequest
                on ChangesRequested
                  ```csharp
                  return new ChangeRequest(context.Key, context.Event.Reason, context.Event.RequestedAt);
                  ```
              query ChangeRequestOfTalk => ChangeRequest optional
                by talkId TalkId
        """;
    const string PerformerQuery = """
              query PerformedChangeRequestOfTalk => ChangeRequest optional
                by talkId TalkId
                performer
                  ```csharp
                  return null;
                  ```
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_render_the_explicitly_constructed_body_without_an_internal_analysis_error(bool performer)
    {
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(Model(ConstructedBody, performer)), self: true);
        AssertNoInternalReducerError(plan);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" ||
            diagnostic.Message.StartsWith("Reducer 'OpenChangeRequests'", StringComparison.Ordinal));
        // The performer has a separate typed-context admission failure (#826), outside reducer analysis.
        if (performer) return;
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Equal(ArtifactRenderOutcome.Rendered, plan.Outcome);
        var artifact = Assert.Single(plan.Artifacts.Where(candidate => candidate.RelativePath.EndsWith("/ChangeRequests.cs", StringComparison.Ordinal)));
        var rendered = Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
        Assert.Contains(ConstructedBody, rendered, StringComparison.Ordinal);
        Assert.Contains("Reduce(", rendered, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_report_the_original_body_conversion_as_a_body_error(bool performer)
    {
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(Model(IssueBody, performer)), self: true);
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        AssertNoInternalReducerError(plan);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" &&
            diagnostic.Message.Contains("Reducer body", StringComparison.Ordinal) && diagnostic.Message.Contains("CS1503", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("Use new Reason(...)", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_suggest_explicit_talk_id_construction_for_the_named_implicit_argument(bool performer)
    {
        var body = ConstructedBody.Replace("new TalkId(context.Key)", "context.Key", StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(Model(body, performer)), self: true);
        Assert.False(plan.Success);
        AssertNoInternalReducerError(plan);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" &&
            diagnostic.Message.Contains("CS1503", StringComparison.Ordinal) && diagnostic.Message.Contains("Use new TalkId(...)", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_keep_refusing_impure_calls_in_the_body(bool performer)
    {
        const string body = "_ = Guid.NewGuid(); return context.State;";
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(Model(body, performer)), self: true);
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        AssertNoInternalReducerError(plan);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" && diagnostic.Message.Contains("NewGuid", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, "Math", "_ = Math.Abs(-1m);")]
    [InlineData(true, "Math", "_ = Math.Abs(-1m);")]
    public async Task should_keep_refusing_a_generated_concept_shadowing_a_bcl_type(bool performer, string name, string expression)
    {
        var source = $"concept {name} : String\n" + Model(expression + " " + ConstructedBody, performer);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source), self: true);
        Assert.False(plan.Success);
        AssertNoInternalReducerError(plan);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022" &&
            diagnostic.Message.Contains($"Generated type '{name}' shadows", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_keep_refusing_a_generated_concept_named_like_a_primitive_as_ambiguous(bool performer)
    {
        var source = "concept Int32 : String\n" + Model("_ = new Int32(context.Key); " + ConstructedBody, performer);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source), self: true);
        Assert.False(plan.Success);
        Assert.Empty(plan.Artifacts);
        AssertNoInternalReducerError(plan);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" &&
            diagnostic.Message.Contains("CS0104", StringComparison.Ordinal) && diagnostic.Message.Contains("'Int32'", StringComparison.Ordinal));
    }

    static void AssertNoInternalReducerError(ArtifactRenderPlan plan) =>
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-021" &&
            (diagnostic.Message.Contains("Internal reducer analysis compilation error", StringComparison.Ordinal) ||
                diagnostic.Message.Contains("CS0234", StringComparison.Ordinal)));

    // A plain keyed query uses IReadModels only. Include a performed sibling to exercise the
    // Arc dependency signatures retained by self-reducer analysis, without changing that query.
    static string Model(string body, bool performer) => Source.Replace(IssueBody, body, StringComparison.Ordinal) + (performer ? "\n" + PerformerQuery : string.Empty);
}
#endif
