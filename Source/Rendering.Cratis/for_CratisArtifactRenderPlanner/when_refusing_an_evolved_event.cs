// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_an_evolved_event : a_v4_reducer
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    [Fact] void should_retain_the_v4_model() => _request.Model.SemanticVersion.ShouldEqual(SemanticVersion.V4);
    [Fact] void should_refuse_without_migration_rendering() => _plan.Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-026");
    [Fact] void should_locate_the_evolved_event() => _plan.Diagnostics.Single().Artifact.ShouldEqual(_event.Id);
    [Fact] void should_emit_no_event_or_reducer_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_refuse_the_reducer_dependency_outside_the_selected_slice()
    {
        var context = new SemanticApplicationContext(_request, new("Projects", "Projects"));
        var reducer = context.SelectedSlices().Single(located => !located.Slice.Reducers.IsEmpty);
        var request = _request with { Scope = new(ArtifactRenderScopeKind.Slice, reducer.Slice.Id) };
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        plan.Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-026");
        plan.Artifacts.ShouldBeEmpty();
    }
}
