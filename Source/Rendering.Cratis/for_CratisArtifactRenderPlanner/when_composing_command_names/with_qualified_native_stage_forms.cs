// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names;

public class with_qualified_native_stage_forms : given.a_two_module_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        DuplicateCommandName();
        CarryAuthoredScene(true, commandComponent: "Stage:commandForm");
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_accept_the_qualified_references() => _plan.Success.ShouldBeTrue();
    [Fact] void should_report_no_ambiguity() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-SCREEN-008").ShouldBeFalse();
    [Fact] void should_emit_the_application() => _plan.Artifacts.ShouldNotBeEmpty();
}
