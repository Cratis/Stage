// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_event_context_mappings;

public class and_an_omitted_mapping_targets_a_required_nested_leaf : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _strict = null!;

    void Because()
    {
        _plan = event_context_projection_model.Plan(event_context_projection_model.RequiredNestedLeaf);
        _strict = event_context_projection_model.Plan(event_context_projection_model.RequiredNestedLeaf, strict: true);
    }

    // The composite is shared, so its leaf is not made nullable for one view: the refusal is kept instead of rendering an invalid backend.
    [Fact] void should_refuse_the_projection() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_an_error() => _plan.Diagnostics.Any(_ => _.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_not_produce_a_degradation() => _plan.Degradations.IsEmpty.ShouldBeTrue();
    [Fact] void should_refuse_when_strict() => _strict.Success.ShouldBeFalse();
}
