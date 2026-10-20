// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Profiles;
using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiProfileConverter.when_converting;

public class and_the_profile_declares_an_identity_corners_and_font : Specification
{
    const string Source =
        """
        ui profile Shop
          target platform web
          identity
            name "Acme Shop"
            logo image "data:image/svg+xml;base64,PHN2Zz48L3N2Zz4="
            show name false
          appearance
            color scheme light
            accent "#c2410c"
            corners round
            font source-serif-4

        ui profile Floor
          target platform web
          identity
            logo icon package
        """;

    UiProfile _shop = null!;
    UiProfile _floor = null!;

    void Because()
    {
        var compiled = new ScreenplayCompiler().Compile(Source);
        var profiles = compiled.Value!.UiProfiles!.ToArray();
        _shop = UiProfileConverter.Convert(profiles[0]).Single();
        _floor = UiProfileConverter.Convert(profiles[1]).Single();
    }

    [Fact] void should_carry_the_name() => _shop.Identity!.Name.ShouldEqual("Acme Shop");
    [Fact] void should_carry_the_image_logo() => (_shop.Identity!.Logo!.Kind, _shop.Identity.Logo.Image).ShouldEqual((LogoKind.Image, "data:image/svg+xml;base64,PHN2Zz48L3N2Zz4="));
    [Fact] void should_carry_that_the_name_is_hidden() => _shop.Identity!.ShowName.ShouldEqual(false);
    [Fact] void should_carry_the_corners() => _shop.Appearance!.Corners.ShouldEqual(CornerStyle.Round);
    [Fact] void should_carry_the_font() => _shop.Appearance!.Font.ShouldEqual("source-serif-4");
    [Fact] void should_carry_an_icon_logo() => (_floor.Identity!.Logo!.Kind, _floor.Identity.Logo.Icon).ShouldEqual((LogoKind.Icon, "package"));
    [Fact] void should_leave_the_name_to_the_application() => (_floor.Identity!.Name, _floor.Identity.ShowName).ShouldEqual((null, null));
}
