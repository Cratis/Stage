// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Profiles;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_target_selects_a_shell_a_package_provides : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish()
    {
        _catalog = [.. _catalog.Select(package => package.Name == "PrimeReact" ? package with { Layouts = ["MobileShell"] } : package)];
        _application = _application with
        {
            UiProfiles = [new UiProfile("Admin", "ios", ["PrimeReact"], null, "MobileShell", "Aurora")],
        };
    }

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_resolve_the_shell_the_package_names() => _result.Targets[0].LayoutName.ShouldEqual("MobileShell");
    [Fact] void should_not_report_it_as_missing() => _result.Targets[0].Findings.Any(finding => finding.Kind == RenderFindingKind.LayoutNotFound).ShouldBeFalse();
    [Fact] void should_carry_no_structure_for_it() => _result.Targets[0].Layout.ShouldBeNull();
    [Fact] void should_place_no_screen_template_against_a_structure_it_cannot_see() => _result.Targets[0].ScreenTemplates.Placements.ShouldBeEmpty();
}
