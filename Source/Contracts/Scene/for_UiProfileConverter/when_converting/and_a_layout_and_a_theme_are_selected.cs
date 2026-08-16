// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_UiProfileConverter.when_converting;

public class and_a_layout_and_a_theme_are_selected : Specification
{
    UiProfileSyntax _syntax = null!;
    List<Cratis.Scene.Model.Profiles.UiProfile> _result = null!;

    void Establish() => _syntax = new(
        "Admin",
        ["web", "ios"],
        null,
        ["PrimeReact"],
        SourceLocation.Start,
        Theme: "Aurora",
        Layout: "AppShell");

    void Because() => _result = [.. UiProfileConverter.Convert(_syntax)];

    [Fact] void should_carry_the_selected_layout_on_every_platform() => _result.TrueForAll(profile => profile.Layout == "AppShell").ShouldBeTrue();
    [Fact] void should_carry_the_selected_theme_on_every_platform() => _result.TrueForAll(profile => profile.Theme == "Aurora").ShouldBeTrue();
}
