// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class with_an_unrelated_error : given.an_unsupported_event_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish() => ChangeIndependent(slice => slice with { Commands = [], Specifications = [] });
    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_keep_the_unrelated_error() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-004").ShouldBeTrue();
    [Fact] void should_keep_the_root_error() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Artifact == _unsupportedEvent).ShouldBeTrue();
    [Fact] void should_refuse_the_whole_render() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_not_publish_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_publish_removals() => _plan.RequiredRemovals.ShouldBeEmpty();
    [Fact] void should_not_publish_degradations() => _plan.Degradations.ShouldBeEmpty();
}
