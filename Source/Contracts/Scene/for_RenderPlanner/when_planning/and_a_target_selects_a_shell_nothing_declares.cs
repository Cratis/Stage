// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Profiles;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_target_selects_a_shell_nothing_declares : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with
    {
        UiProfiles = [new UiProfile("Admin", "ios", ["PrimeReact"], null, "MobileShell", "Aurora")],
    };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_report_the_shell_as_not_found() => Finding()!.Kind.ShouldEqual(RenderFindingKind.LayoutNotFound);
    [Fact] void should_name_the_shell_the_target_selected() => Finding()!.Subject.ShouldEqual("MobileShell");
    [Fact] void should_resolve_no_shell_name() => _result.Targets[0].LayoutName.ShouldBeNull();
    [Fact] void should_resolve_no_shell_structure() => _result.Targets[0].Layout.ShouldBeNull();
    [Fact] void should_place_no_screen_template_against_a_shell_it_does_not_have() => _result.Targets[0].ScreenTemplates.Placements.ShouldBeEmpty();

    RenderFinding? Finding() => _result.Targets[0].Findings.FirstOrDefault(finding => finding.Kind == RenderFindingKind.LayoutNotFound);
}
