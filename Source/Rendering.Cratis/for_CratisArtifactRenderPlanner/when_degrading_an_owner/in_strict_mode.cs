// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class in_strict_mode : given.an_unsupported_event_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        _request = _request with { Profile = CratisRendering.CreateProfile(_model.Application.Name, _options with { Strict = true }) };
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_refuse_the_whole_render() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_preserve_the_original_error() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Artifact == _unsupportedEvent && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_not_publish_omission_evidence() => _plan.Degradations.ShouldBeEmpty();
}
