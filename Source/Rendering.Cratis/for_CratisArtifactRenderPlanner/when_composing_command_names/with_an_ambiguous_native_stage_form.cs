// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names;

public class with_an_ambiguous_native_stage_form : given.a_two_module_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        DuplicateCommandName();
        CarryAuthoredScene(false, commandComponent: "Stage:commandForm");
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_report_both_ambiguous_command_candidates() => _plan.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-SCREEN-008").ShouldEqual(2);
    [Fact] void should_refuse_the_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
