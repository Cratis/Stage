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

    [Fact] void should_emit_an_action_component() => _result.ComponentName.ShouldEqual("core:guarded-action");
    [Fact] void should_carry_the_label() => _result.Properties["label"].ShouldEqual(_action.Label);
    [Fact] void should_carry_the_alternatives() => Alternatives.Count.ShouldEqual(1);
    [Fact] void should_carry_the_alternative_command() => Alternatives[0].Command.ShouldEqual("SendInvoice");
    [Fact] void should_carry_the_condition() => ((Cratis.Scene.Model.Guards.ComparisonGuardCondition)Alternatives[0].Condition).Operator.ShouldEqual(Cratis.Scene.Model.Guards.GuardOperator.Equal);
    [Fact] void should_preserve_an_unqualified_path_rather_than_invent_a_subject() => ((Cratis.Scene.Model.Guards.ComparisonGuardCondition)Alternatives[0].Condition).Left.Subject.ShouldEqual("status");
    [Fact] void should_keep_an_unqualified_path_unevaluable() => SceneGuards.CanEvaluate(Alternatives[0].Condition).ShouldBeFalse();
    [Fact] void should_not_invent_a_fallback() => _result.Properties.ContainsKey("otherwise").ShouldBeFalse();

    IReadOnlyList<Cratis.Scene.Model.Guards.GuardedActionAlternative> Alternatives => Cratis.Scene.Model.Guards.GuardedActionProperties.Read(_result).Alternatives;
}
