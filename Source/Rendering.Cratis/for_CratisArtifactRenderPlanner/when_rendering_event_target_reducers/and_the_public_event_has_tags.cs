// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_event_target_reducers;

public class and_the_public_event_has_tags : Specification
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _plan = null!;

    async Task Establish() => _loaded = await when_rendering_a_pure_reducer.Load(
        and_a_reducer_publishes_a_public_event.Declaration.Replace("public event OrderPublished\n", "public event OrderPublished\n        tag \"published\"\n", StringComparison.Ordinal) + "\n" + and_a_reducer_publishes_a_public_event.Reducer);

    void Because() => _plan = when_rendering_a_pure_reducer.Plan(_loaded);

    [Fact] void should_refuse_the_unsupported_sink_metadata() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-032" && diagnostic.Message.Contains("tags", StringComparison.Ordinal));
    [Fact] void should_not_render_the_reducer() => Assert.DoesNotContain(_plan.Artifacts, artifact => artifact.RelativePath.EndsWith("OrderPublisher.cs", StringComparison.Ordinal));
}
#endif
