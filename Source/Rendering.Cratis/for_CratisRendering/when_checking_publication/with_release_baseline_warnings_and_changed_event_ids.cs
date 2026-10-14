// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_checking_publication;

public class with_release_baseline_warnings_and_changed_event_ids : a_release_baseline_request
{
    CratisPublicationCheck _check = null!;
    void Establish() => _plan = _planner.Plan(WithBaseline(_assessment));
    void Because() => _check = CratisRendering.CheckPublication(_plan, path => _plan.Artifacts.FirstOrDefault(artifact => artifact.RelativePath == path) is { } artifact ? Text(artifact).Replace("[global::Cratis.Chronicle.Events.EventTypeAttribute]", "[global::Cratis.Chronicle.Events.EventTypeAttribute(\"PreviousStoredName\")]", StringComparison.Ordinal) : null);
    [Fact] void should_still_refuse_publication() => _check.ShouldBeOfExactType<CratisPublicationCheck.ChangesEventTypeIds>();
    [Fact] void should_preserve_the_event_id_error() => ((CratisPublicationCheck.ChangesEventTypeIds)_check).Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-EVENT-002");
}
#endif
