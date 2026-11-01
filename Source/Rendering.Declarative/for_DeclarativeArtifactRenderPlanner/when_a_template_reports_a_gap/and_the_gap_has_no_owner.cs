// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_a_template_reports_a_gap;

public class and_the_gap_has_no_owner : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderRequest _request = null!;

    void Because()
    {
        _plan = PlanWith(new() { ["templates/index.sbn"] = "{{ gap 'disabled_specification' 'Missing index' 'model.unknown' }}" });
        _request = Request(Profile(Archive()));
    }

    [Fact] void should_record_the_gap_as_plan_evidence() => _plan.PlanEvidence.Length.ShouldEqual(1);
    [Fact] void should_attribute_it_to_the_application() => _plan.PlanEvidence[0].Diagnostic.Artifact.ShouldEqual(_request.Model.Application.Id);
    [Fact] void should_record_no_item_degradation() => _plan.Degradations.ShouldBeEmpty();
    [Fact] void should_still_render_the_artifact() => Text(_plan, "Index.test").ShouldEqual("/* DisabledSpecification STAGE-DEGRADE-002 */");
}
