// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_guarded_action_has_a_hidden_fallback : given.a_guarded_action
{
    ExternalComponent _result = null!;

    void Establish() => _action = _action with { Otherwise = new ScreenActionOtherwiseSyntax(ScreenActionOtherwiseOutcome.Hidden, null, _location) };
    void Because() => _result = (ExternalComponent)ScreenDirectiveConverter.Convert([_action], "InvoiceDetails")[0];

    [Fact] void should_emit_an_action_component() => _result.ComponentName.ShouldEqual("core:action");
    [Fact] void should_carry_the_hidden_fallback() => Otherwise["outcome"].ShouldEqual("Hidden");
    [Fact] void should_not_invent_a_fallback_command() => Otherwise["command"].ShouldBeNull();

    Dictionary<string, object?> Otherwise => (Dictionary<string, object?>)_result.Properties["otherwise"]!;
}
