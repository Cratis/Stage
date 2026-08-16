// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_component_resolves_against_nothing : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with
    {
        Screens =
        [
            .. _application.Screens,
            new Screen(
                "Dashboard",
                "AppShell",
                new Dictionary<string, IReadOnlyList<SceneElement>>(StringComparer.Ordinal)
                {
                    ["main"] = [SceneElementFactory.Component("Dashboard.main.0-chart", "core:chart")],
                },
                [],
                [],
                "MasterDetail"),
        ],
    };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_not_consider_the_application_renderable() => _result.IsComplete.ShouldBeFalse();
    [Fact] void should_report_the_unresolved_component_on_every_target() => _result.Targets.All(target => Finding(target) is not null).ShouldBeTrue();
    [Fact] void should_name_the_component_that_did_not_resolve() => Finding(_result.Targets[0])!.Subject.ShouldEqual("core:chart");
    [Fact] void should_leave_it_out_of_the_resolved_components() => _result.Targets[0].Components.Any(component => component.Name == "core:chart").ShouldBeFalse();
    [Fact] void should_still_resolve_the_components_that_do_resolve() => _result.Targets[0].Components.Count.ShouldEqual(2);

    static RenderFinding? Finding(RenderPlan target) =>
        target.Findings.FirstOrDefault(finding => finding.Kind == RenderFindingKind.ComponentNotResolved);
}
