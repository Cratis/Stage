// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Profiles;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene.for_RenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner.when_planning;

public class and_the_theme_is_incompatible_with_a_package : an_application_targeting_web_and_ios
{
    ApplicationRenderPlan _result = null!;

    void Establish() => _application = _application with { Themes = [new Theme("Aurora", ["core", "PrimeReact"])] };

    void Because() => _result = RenderPlanner.Plan(_application, _catalog);

    [Fact] void should_not_consider_the_application_renderable() => _result.IsComplete.ShouldBeFalse();
    [Fact] void should_report_the_pairing() => Finding()!.Kind.ShouldEqual(RenderFindingKind.ThemeIncompatible);
    [Fact] void should_name_the_package_the_theme_does_not_cover() => Finding()!.Subject.ShouldEqual("Tailwind");
    [Fact] void should_still_apply_the_theme() => _result.Targets[0].Theme!.Name.ShouldEqual("Aurora");
    [Fact] void should_scope_the_tokens_to_the_packages_it_does_cover() => _result.Targets[0].ThemePackages.ShouldContainOnly("PrimeReact");

    RenderFinding? Finding() => _result.Targets[0].Findings.FirstOrDefault(finding => finding.Kind == RenderFindingKind.ThemeIncompatible);
}
