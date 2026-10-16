// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_target_roots_on_an_own_shell : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with
    {
        UiProfiles = [new UiProfile("Admin", "web", ["PrimeReact"], null, null, "Aurora", Root: new RootTemplate(new TemplateReference(TemplateReferenceSource.Own, "AppShell")))],
    };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_resolve_the_effective_shell_through_the_scene_engine() => _result.Targets[0].Shell!.Name.ShouldEqual("AppShell");
    [Fact] void should_render_inside_the_application_s_own_layout() => _result.Targets[0].Layout!.Name.ShouldEqual("AppShell");
    [Fact] void should_have_no_findings() => _result.Targets[0].Findings.ShouldBeEmpty();
}
