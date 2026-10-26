// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names;

public class with_an_ambiguous_authored_reference : given.a_two_module_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        DuplicateCommandName();
        CarryAuthoredScene(false);
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_report_ambiguity() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-SCREEN-008" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_name_the_command_and_each_participating_slice() => _plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-SCREEN-008").Select(diagnostic => diagnostic.Message).All(message => message.Contains("RegisterProject", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_identify_both_slices() => _plan.Diagnostics.Count(diagnostic => diagnostic.Code == "STAGE-SCREEN-008").ShouldEqual(2);
    [Fact] void should_refuse_the_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
