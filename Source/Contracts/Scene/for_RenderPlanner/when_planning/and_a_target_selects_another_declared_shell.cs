// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Scene.Model.Profiles;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_target_selects_another_declared_shell : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with
    {
        Layouts = [.. _application.Layouts, new Layout("MobileShell", [new Slot("content")])],
        UiProfiles = [new UiProfile("Admin", "ios", ["PrimeReact"], null, "MobileShell", "Aurora")],
    };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_render_inside_the_shell_the_target_selects() => _result.Targets[0].Layout!.Name.ShouldEqual("MobileShell");
    [Fact] void should_report_the_screen_left_on_the_other_shell() => Finding()!.Kind.ShouldEqual(RenderFindingKind.ScreenNotOnSelectedLayout);
    [Fact] void should_name_the_screen_that_did_not_follow() => Finding()!.Subject.ShouldEqual("InvoiceDetails");

    RenderFinding? Finding() =>
        _result.Targets[0].Findings.FirstOrDefault(finding => finding.Kind == RenderFindingKind.ScreenNotOnSelectedLayout);
}
