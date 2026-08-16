// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_screen_template_fits_a_slot_nothing_declares : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with
    {
        ScreenTemplates = [_application.ScreenTemplates[0] with { FitsSlot = "workspace" }],
    };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_not_consider_the_application_renderable() => _result.IsComplete.ShouldBeFalse();
    [Fact] void should_report_the_template_as_unplaced() => Finding()!.Kind.ShouldEqual(RenderFindingKind.ScreenTemplateUnplaced);
    [Fact] void should_name_the_template_that_found_no_home() => Finding()!.Subject.ShouldEqual("MasterDetail");
    [Fact] void should_place_nothing() => _result.Targets[0].ScreenTemplates.Placements.ShouldBeEmpty();

    RenderFinding? Finding() =>
        _result.Targets[0].Findings.FirstOrDefault(finding => finding.Kind == RenderFindingKind.ScreenTemplateUnplaced);
}
