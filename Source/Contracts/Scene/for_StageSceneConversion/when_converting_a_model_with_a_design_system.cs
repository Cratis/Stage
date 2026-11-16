// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_StageSceneConversion;

public class when_converting_a_model_with_a_design_system : given.a_model_with_a_design_system_and_free_screens
{
    void Because() => _scene = StageSceneConversion.Visit(_application);

    [Fact] void should_carry_the_design_system_on_the_profile() => _scene.UiProfiles.Single().DesignSystem!.Name.ShouldEqual("ShopDesign");
    [Fact] void should_resolve_the_theme_that_implements_it() => _scene.UiProfiles.Single().DesignSystem!.Themes.Single().Name.ShouldEqual("Daylight");
    [Fact] void should_resolve_the_token_values_of_the_theme_in_each_mode() => _scene.UiProfiles.Single().DesignSystem!.Themes.Single().Modes.Single(mode => mode.Mode == "dark").Tokens["color.surface"].ShouldEqual("#111827");
    [Fact] void should_resolve_the_parts_of_a_component_to_values() => _scene.UiProfiles.Single().DesignSystem!.Themes.Single().Modes.Single(mode => mode.Mode == "light").Components!.Single().Variants.Single().Parts["background"].ShouldEqual("#4338CA");
    [Fact] void should_keep_the_color_scheme_the_profile_selects() => _scene.UiProfiles.Single().Appearance!.ColorScheme.ShouldEqual(Cratis.Scene.Model.Profiles.ColorScheme.Dark);
}
