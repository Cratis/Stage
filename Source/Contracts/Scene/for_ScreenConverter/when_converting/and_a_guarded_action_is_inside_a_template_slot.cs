// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting;

public class and_a_guarded_action_is_inside_a_template_slot : for_ScreenDirectiveConverter.given.a_guarded_action
{
    ScreenSyntax _screen = null!;
    ExternalComponent _result = null!;

    void Establish() => _screen = new(
        "InvoiceDetails",
        null,
        [new ScreenTemplateReferenceSyntax("Details", [new ScreenSlotSyntax("actions", [_action], _location)], _location)],
        _location);

    void Because() => _result = (ExternalComponent)ScreenConverter.Convert(_screen, "AppShell", [], []).SlotContent["actions"][0];

    [Fact] void should_keep_the_guarded_action_in_the_slot() => _result.ComponentName.ShouldEqual("core:action");
    [Fact] void should_keep_the_guarded_action_alternatives() => _result.Properties.ContainsKey("alternatives").ShouldBeTrue();
}
