// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTriggerArtifactRenderer;

public class when_rendering_a_trigger_type_collision : given.an_application_trigger_reaction
{
    void Establish()
    {
        var trigger = SemanticOccurrenceAdmission.Trigger(_reaction, _context);
        SetContext(ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with
        {
            Triggers = [trigger with { Name = "BatchStarted" }]
        }));
    }

    void Because() => Plan();

    [Fact] void should_refuse_the_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_not_emit_files() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_name_the_collision() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-012");
}
