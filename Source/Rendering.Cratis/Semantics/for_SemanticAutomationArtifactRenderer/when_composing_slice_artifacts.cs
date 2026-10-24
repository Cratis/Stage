// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer;

[PerFactSpecificationLifecycle]
public class when_composing_slice_artifacts : given.an_automation
{
    [Fact]
    public void should_admit_an_automation_without_commands()
    {
        ReplaceAutomation(_located.Slice with { Commands = [], Reactions = [_located.Slice.Reactions.Single(reaction => reaction.Name == "Acknowledge")] });
        Plan();
        _plan.Success.ShouldBeTrue();
        _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/FollowUp.cs", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void should_admit_and_compile_several_commands_in_one_slice()
    {
        const string command = """
                  command RecordRemark
                    id String identifier
                    remark String
                    produces event Remarked
                      for id
                      remark String = remark
            """;
        SetContext(Compile(Source.Replace("      event Acknowledged", command + "\n      event Acknowledged", StringComparison.Ordinal)));
        Plan();
        _plan.Success.ShouldBeTrue();
        RenderedOutput.Errors(Files()).ShouldBeEmpty();
        var source = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/FollowUp.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        source.ShouldContain("public record CompleteWork");
        source.ShouldContain("public record RecordRemark");
    }

    [Fact]
    public async Task should_render_an_automation_read_model_query_and_reducer()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.Source
            .Replace("slice StateView Totals", "slice Automation Totals", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal));
        _plan = when_rendering_a_pure_reducer.Plan(loaded);
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/Fold.cs", StringComparison.Ordinal)).ShouldBeTrue();
        var source = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Totals.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        source.ShouldContain("public record Total");
        source.ShouldContain("ById(");
        RenderedOutput.Errors(Files()).ShouldBeEmpty();
    }

    [Fact]
    public void should_render_a_projection_in_an_automation_slice()
    {
        const string read = """
                  readmodel Summary
                    id String
                    amount Decimal
                  query ById => Summary?
                    by id String
                  projection SummaryProjection => Summary
                    from Started key id
                      id = id
                      amount = amount
            """;
        SetContext(Compile(("concept WorkId : String\n" + Source + "\n" + read).Replace("id String", "id WorkId", StringComparison.Ordinal)));
        Plan();
        _plan.Success.ShouldBeTrue();
        RenderedOutput.Errors(Files()).ShouldBeEmpty();
        Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/FollowUp.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain("FromEventAttribute<global::Reactions.Work.Processing.Start.Started>");
    }

    RenderedFile[] Files() => [.. _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
}
#endif
