// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting;

public class and_a_guarded_action_is_inside_a_section : for_ScreenDirectiveConverter.given.a_guarded_action
{
    ScreenSyntax _screen = null!;
    Exception? _error;

    void Establish() => _screen = new(
        "InvoiceDetails",
        null,
        [new ScreenSectionSyntax("Actions", [_action], _location)],
        _location);

    void Because() => _error = Catch.Exception(() => ScreenConverter.Convert(_screen, "AppShell", [], []));

    [Fact] void should_refuse_the_nested_action() => _error.ShouldBeOfExactType<UnsupportedGuardedScreenAction>();
}
