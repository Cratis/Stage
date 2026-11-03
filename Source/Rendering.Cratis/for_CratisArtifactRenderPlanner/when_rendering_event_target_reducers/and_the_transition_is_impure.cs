// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_event_target_reducers;

public class and_the_transition_is_impure : Specification
{
    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _plan = null!;

    async Task Establish() => _loaded = await when_rendering_a_pure_reducer.Load(and_a_reducer_publishes_a_public_event.Declaration + "\n" +
        and_a_reducer_publishes_a_public_event.Reducer.Replace("context.Event.Name);", "global::System.IO.File.ReadAllText(\"input.txt\"));", StringComparison.Ordinal));

    void Because() => _plan = when_rendering_a_pure_reducer.Plan(_loaded);

    [Fact] void should_apply_the_existing_purity_rules_to_event_state() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-022");
    [Fact] void should_not_render_an_impure_reducer() => Assert.Empty(_plan.Artifacts);
}
#endif
