// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.Screens;
using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiProfileConverter.when_converting;

public class and_the_profile_declares_an_own_root_and_a_system_scheme : Specification
{
    const string Source =
        """
        ui profile Desktop
          target platform web
          root template AppFrame
          appearance
            color scheme system
        """;

    UiProfile _result = null!;

    void Because() => _result = UiProfileConverter.Convert(new ScreenplayCompiler().Compile(Source).Value!.UiProfiles!.Single()).Single();

    [Fact] void should_name_the_root_as_an_own_template() => _result.Root!.Template.ShouldEqual(new TemplateReference(TemplateReferenceSource.Own, "AppFrame"));
    [Fact] void should_declare_no_options() => _result.Root!.Options.ShouldBeNull();
    [Fact] void should_follow_the_system_colour_scheme() => _result.Appearance!.ColorScheme.ShouldEqual(ColorScheme.System);
    [Fact] void should_declare_no_accent() => _result.Appearance!.Accent.ShouldBeNull();
    [Fact] void should_declare_no_defaults() => _result.Defaults.ShouldBeNull();
}
