// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_event_target_reducers;

public class and_multiple_private_events_are_folded : Specification
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _plan = null!;
    RenderedFile[] _files = [];

    async Task Establish()
    {
        var declaration = and_a_reducer_publishes_a_public_event.Declaration.Replace("    slice Translate Publish", "      event Renamed\n        name String\n    slice Translate Publish", StringComparison.Ordinal);
        const string transition = """
                on Renamed
                  ```csharp
                  return new OrderPublished(context.Event.Name);
                  ```
        """;
        _loaded = await when_rendering_a_pure_reducer.Load(declaration + "\n" + and_a_reducer_publishes_a_public_event.Reducer + "\n" + transition);
    }

    void Because()
    {
        _plan = when_rendering_a_pure_reducer.Plan(_loaded);
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        _files = [.. _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
    }

    [Fact] void should_render_both_overloads() => Assert.Equal(2, _files.Single(file => file.RelativePath.EndsWith("OrderPublisher.cs", StringComparison.Ordinal)).Content.Split("? On(", StringSplitOptions.None).Length - 1);
    [Fact] void should_compile_both_transitions() => Assert.Empty(RenderedOutput.Errors(_files));
}
#endif
