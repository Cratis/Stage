// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_guarded_action_has_a_hidden_fallback : given.a_guarded_action
{
    Exception? _error;

    void Establish() => _action = _action with { Otherwise = new ScreenActionOtherwiseSyntax(ScreenActionOtherwiseOutcome.Hidden, null, _location) };

    void Because() => _error = Catch.Exception(() => ScreenDirectiveConverter.Convert([_action], "InvoiceDetails"));

    [Fact] void should_refuse_instead_of_losing_the_guard() => _error.ShouldBeOfExactType<UnsupportedGuardedScreenAction>();
}
