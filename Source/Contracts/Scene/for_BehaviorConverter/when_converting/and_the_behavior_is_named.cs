// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using SceneModel = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene.for_BehaviorConverter.when_converting;

public class and_the_behavior_is_named : Specification
{
    BehaviorSyntax _syntax = null!;
    SceneModel.Behavior _result = null!;

    void Establish() => _syntax = new(
        "ConfirmThenCancel",
        [],
        [
            new InteractionBindingSyntax(
                new BuiltInInteractionTriggerSyntax(InteractionTriggerKind.Click, SourceLocation.Start),
                "item.canCancel",
                [new ExecuteCommandActionSyntax("CancelInvoice", SourceLocation.Start)],
                SourceLocation.Start)
        ],
        SourceLocation.Start,
        Order: 2);

    void Because() => _result = BehaviorConverter.Convert(_syntax);

    [Fact] void should_carry_the_name() => _result.Name.ShouldEqual("ConfirmThenCancel");
    [Fact] void should_carry_the_order() => _result.Order.ShouldEqual(2);
    [Fact] void should_carry_the_binding() => _result.Bindings.Count.ShouldEqual(1);
    [Fact] void should_carry_the_trigger() => ((SceneModel.BuiltInInteractionTrigger)_result.Bindings[0].Trigger).Kind.ShouldEqual(SceneModel.InteractionTriggerKind.Click);
    [Fact] void should_carry_the_guard_as_a_binding() => _result.Bindings[0].Condition!.Path.ShouldEqual("item.canCancel");
    [Fact] void should_carry_the_action() => ((SceneModel.ExecuteCommandAction)_result.Bindings[0].Actions[0]).Command.ShouldEqual("CancelInvoice");
}
