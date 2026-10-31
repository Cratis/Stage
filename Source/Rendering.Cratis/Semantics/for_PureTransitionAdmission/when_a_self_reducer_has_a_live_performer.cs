// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_PureTransitionAdmission;

public class when_a_self_reducer_has_a_live_performer : Specification
{
    ArtifactRenderPlan _plan = null!;

    async Task Because()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace("query ById => Total?\n        by id Uuid", "query ById => Total?\n        by id Uuid\n      query LiveTotals => observable Total[]\n        performer\n          ```csharp\n          return readModels;\n          ```", StringComparison.Ordinal)
            .Replace("Guid.Parse(\"00000000-0000-0000-0000-000000000001\")", "context.Event.Id", StringComparison.Ordinal);
        var loaded = await when_rendering_a_pure_reducer.Load(source);
        _plan = when_rendering_a_pure_reducer.Plan(loaded, self: true);
    }

    [Fact] void should_admit_the_transition_and_live_performer() => Assert.True(_plan.Success && _plan.Outcome == ArtifactRenderOutcome.Complete, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_keep_the_self_reducer() => Content().ShouldContain("public static Total? Reduce(");
    [Fact] void should_keep_the_live_performer() => Content().ShouldContain("LiveReadModels<Total>");
    [Fact] void should_compile_without_errors() => RenderedOutput.Errors(Files()).ShouldBeEmpty();
    [Fact] void should_compile_without_warnings() => RenderedOutput.Warnings(Files()).ShouldBeEmpty();

    string Content() => Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Totals.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    IEnumerable<RenderedFile> Files() => _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs").Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
}
#endif
