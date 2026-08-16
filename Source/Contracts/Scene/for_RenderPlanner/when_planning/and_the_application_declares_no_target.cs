// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_the_application_declares_no_target : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with { UiProfiles = [] };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_plan_nothing() => _result.Targets.ShouldBeEmpty();
    [Fact] void should_say_there_is_nothing_to_render_for() => _result.Findings.Single().Kind.ShouldEqual(RenderFindingKind.NoTargetDeclared);
    [Fact] void should_not_pass_for_a_renderable_application() => _result.IsComplete.ShouldBeFalse();
}
