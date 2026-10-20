// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_profile_declares_neither_layout_nor_root : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with
    {
        UiProfiles = [_application.UiProfiles[0] with { Layout = null, Root = null }],
        Layouts = [],
        ScreenTemplates = [],
        Screens = [_application.Screens[0] with { Layout = string.Empty, ScreenTemplate = null }]
    };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_plan_successfully() => _result.IsComplete.ShouldBeTrue();
    [Fact] void should_report_no_target_findings() => _result.Targets.Single().Findings.ShouldBeEmpty();
    [Fact] void should_leave_the_layout_name_for_the_default_blueprint() => _result.Targets.Single().LayoutName.ShouldBeNull();
    [Fact] void should_not_invent_a_layout() => _result.Targets.Single().Layout.ShouldBeNull();
    [Fact] void should_keep_the_application_layout_free() => _application.Layouts.ShouldBeEmpty();
    [Fact] void should_keep_the_screens_layout_name_empty() => _application.Screens.Single().Layout.ShouldEqual(string.Empty);
}
