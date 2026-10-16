// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.Screens;
using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiProfileConverter.when_converting;

public class and_a_root_template_appearance_and_defaults_are_declared : Specification
{
    const string Source =
        """
        ui profile Desktop
          target platform web
          root template blueprint Cratis.Blueprint.Default AppShell version "^4.5"
            option navigationMode slim
            option showFooter true
            option title "Back office"
            option density 2
          appearance
            color scheme dark
            accent "#4f46e5"
          new screens use template blueprint crm Workspace version "^2.0"
        """;

    UiProfile _result = null!;

    void Because()
    {
        var compiled = new ScreenplayCompiler().Compile(Source);
        _result = UiProfileConverter.Convert(compiled.Value!.UiProfiles!.Single()).Single();
    }

    [Fact] void should_name_the_root_template_as_a_pinned_blueprint_reference() => _result.Root!.Template.ShouldEqual(new TemplateReference(TemplateReferenceSource.Blueprint, "AppShell", "Cratis.Blueprint.Default", "^4.5"));
    [Fact] void should_carry_a_word_option_as_text() => _result.Root!.Options!["navigationMode"]!.GetValue<string>().ShouldEqual("slim");
    [Fact] void should_carry_a_flag_option_as_a_boolean() => _result.Root!.Options!["showFooter"]!.GetValue<bool>().ShouldBeTrue();
    [Fact] void should_carry_a_quoted_option_as_text() => _result.Root!.Options!["title"]!.GetValue<string>().ShouldEqual("Back office");
    [Fact] void should_carry_a_number_option_as_a_number() => _result.Root!.Options!["density"]!.GetValue<long>().ShouldEqual(2L);
    [Fact] void should_carry_the_colour_scheme() => _result.Appearance!.ColorScheme.ShouldEqual(ColorScheme.Dark);
    [Fact] void should_carry_the_accent() => _result.Appearance!.Accent.ShouldEqual("#4f46e5");
    [Fact] void should_carry_the_template_new_screens_start_from() => _result.Defaults!.NewScreenTemplate.ShouldEqual(new TemplateReference(TemplateReferenceSource.Blueprint, "Workspace", "crm", "^2.0"));
    [Fact] void should_leave_the_legacy_layout_unset() => _result.Layout.ShouldBeNull();
}
