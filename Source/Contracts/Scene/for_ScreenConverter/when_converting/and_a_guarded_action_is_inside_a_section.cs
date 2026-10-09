// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting;

public class and_a_guarded_action_is_inside_a_section : for_ScreenDirectiveConverter.given.a_guarded_action
{
    ScreenSyntax _screen = null!;
    ExternalComponent _section = null!;

    void Establish() => _screen = new(
        "InvoiceDetails",
        null,
        [new ScreenSectionSyntax("Actions", [_action], _location)],
        _location);

    void Because() => _section = (ExternalComponent)ScreenConverter.Convert(_screen, "AppShell", [], []).SlotContent["content"][0];

    [Fact] void should_keep_the_section() => _section.ComponentName.ShouldEqual("core:section");
    [Fact] void should_keep_the_guarded_action_inside_the_section() => Action.ComponentName.ShouldEqual("core:action");
    [Fact] void should_keep_the_guarded_action_alternatives() => Action.Properties.ContainsKey("alternatives").ShouldBeTrue();

    ExternalComponent Action => (ExternalComponent)_section.Slots["content"][0];
}
