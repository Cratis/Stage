// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.Screens;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_a_target_roots_on_an_unknown_shell : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with
    {
        UiProfiles = [new UiProfile("Admin", "web", ["PrimeReact"], null, null, "Aurora", Root: new RootTemplate(new TemplateReference(TemplateReferenceSource.Own, "Nowhere")))],
    };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_report_the_root_as_invalid_at_build_time() => _result.Targets[0].Findings.Count(finding => finding.Kind == RenderFindingKind.RootTemplateInvalid).ShouldEqual(1);
    [Fact] void should_name_the_template_in_the_finding() => _result.Targets[0].Findings.Single(finding => finding.Kind == RenderFindingKind.RootTemplateInvalid).Subject.ShouldEqual("Nowhere");
    [Fact] void should_not_be_complete() => _result.Targets[0].IsComplete.ShouldBeFalse();
    [Fact] void should_not_also_report_a_missing_layout() => _result.Targets[0].Findings.Any(finding => finding.Kind == RenderFindingKind.LayoutNotFound).ShouldBeFalse();
}
