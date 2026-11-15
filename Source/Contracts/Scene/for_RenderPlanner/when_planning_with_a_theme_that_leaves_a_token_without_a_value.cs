// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_RenderPlanner;

public class when_planning_with_a_theme_that_leaves_a_token_without_a_value : for_StageSceneConversion.given.a_model_with_a_design_system_and_free_screens
{
    IEnumerable<RenderFinding> _findings = null!;

    void Because()
    {
        var incomplete = Source.Replace("    color.on-primary = palette.white\n\n  dark", "\n  dark");
        var scene = StageSceneConversion.Visit(new Cratis.Screenplay.ScreenplayCompiler().Parse(incomplete).Value!);
        _findings = RenderFindings.ForDesignSystem(scene.UiProfiles.Single(), "Web");
    }

    [Fact] void should_find_the_design_system_cannot_resolve_the_theme() => _findings.Select(finding => finding.Kind).Distinct().ShouldContainOnly(RenderFindingKind.DesignSystemUnresolved);
    [Fact] void should_name_the_theme_and_the_mode_it_cannot_resolve() => _findings.Select(finding => finding.Subject).ShouldContainOnly("ShopDesign/Daylight/light");
}
