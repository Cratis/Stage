// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_degrading_an_owner;

public class within_a_newer_esm_minor : given.an_unsupported_event_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        Replace(_model, "LanguageVersion", new LanguageVersion(32, 1));
        Replace(_model, "SemanticVersion", new SemanticVersion(32, 1));
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_refuse_the_whole_render() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_keep_the_security_error() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-016" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_keep_the_range_warning() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == ArtifactRenderDegradationCodes.NewerEsmWithinDeclaredRange && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_not_publish_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_publish_removals() => _plan.RequiredRemovals.ShouldBeEmpty();
    [Fact] void should_not_degrade_the_root_or_dependents() => _plan.Diagnostics.Any(diagnostic => string.Equals(diagnostic.Code, "STAGE-DEGRADE-001", StringComparison.Ordinal) || string.Equals(diagnostic.Code, "STAGE-DEGRADE-005", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_publish_omission_refusals() => _plan.Refusals.ShouldBeEmpty();
}
