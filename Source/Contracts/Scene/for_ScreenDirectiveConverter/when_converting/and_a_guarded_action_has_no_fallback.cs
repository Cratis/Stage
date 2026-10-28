// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.when_converting;

public class and_a_guarded_action_has_no_fallback : given.a_guarded_action
{
    ExternalComponent _result = null!;

    void Because() => _result = (ExternalComponent)ScreenDirectiveConverter.Convert([_action], "InvoiceDetails")[0];

    [Fact] void should_emit_an_action_component() => _result.ComponentName.ShouldEqual("core:action");
    [Fact] void should_carry_the_label() => _result.Properties["label"].ShouldEqual(_action.Label);
    [Fact] void should_carry_the_alternatives() => Alternatives.Count.ShouldEqual(1);
    [Fact] void should_carry_the_alternative_command() => Alternatives[0]["command"].ShouldEqual("SendInvoice");
    [Fact] void should_carry_the_condition() => ((Dictionary<string, object?>)Alternatives[0]["condition"]!)["operator"].ShouldEqual("Equal");
    [Fact] void should_not_invent_a_fallback() => _result.Properties.ContainsKey("otherwise").ShouldBeFalse();

    List<Dictionary<string, object?>> Alternatives => (List<Dictionary<string, object?>>)_result.Properties["alternatives"]!;
}
