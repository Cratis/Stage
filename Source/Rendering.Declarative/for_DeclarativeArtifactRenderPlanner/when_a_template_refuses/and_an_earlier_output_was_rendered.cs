// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_a_template_refuses;

public class and_an_earlier_output_was_rendered : given.a_declarative_planner
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = PlanWith(new() { ["templates/reaction.sbn"] = "{{ refuse 'trust' 'unsafe' }}" });

    [Fact] void should_discard_every_artifact() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_report_the_refusal() => _plan.Diagnostics.Select(_ => _.Code).ShouldEqual([TemplateCodes.Refuse]);
    [Fact] void should_not_succeed() => _plan.Success.ShouldBeFalse();
}
