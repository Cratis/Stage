// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_a_template_reports_a_gap;

public class and_the_gap_names_a_construct : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanWith(new() { ["templates/index.sbn"] = "{{ gap 'unregistered_consumer' 'Missing consumer' model.items.reactions[0].id }}" });

    [Fact] void should_attribute_it_to_that_construct() => _plan.Degradations.Single().Diagnostic.Artifact.ShouldEqual(_plan.Artifacts.Single(_ => _.RelativePath == "Reactions/EchoReaction.test").Sources.Single());
    [Fact] void should_not_record_plan_evidence() => _plan.PlanEvidence.ShouldBeEmpty();
    [Fact] void should_not_attribute_it_to_the_application() => _plan.Degradations.Single().Diagnostic.Artifact.ShouldNotEqual(Request(Profile(Archive())).Model.Application.Id);
}
