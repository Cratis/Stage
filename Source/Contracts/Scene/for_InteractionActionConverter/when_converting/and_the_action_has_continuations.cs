// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using SceneModel = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene.for_InteractionActionConverter.when_converting;

public class and_the_action_has_continuations : Specification
{
    InteractionActionSyntax _syntax = null!;
    SceneModel.InteractionAction _result = null!;

    void Establish() => _syntax = new ExecuteCommandActionSyntax("RegisterInvoice", SourceLocation.Start)
    {
        Arguments = [new InteractionArgumentSyntax("invoiceId", "item.invoiceId", SourceLocation.Start)],
        OnSuccess = [new CloseDialogActionSyntax(SourceLocation.Start)],
        OnFailure = [new NotifyActionSyntax(NotificationLevel.Error, "It failed", SourceLocation.Start)]
    };

    void Because() => _result = InteractionActionConverter.Convert(_syntax);

    [Fact] void should_identify_the_kind() => _result.Kind.ShouldEqual(SceneModel.InteractionActionKind.ExecuteCommand);
    [Fact] void should_carry_the_command() => ((SceneModel.ExecuteCommandAction)_result).Command.ShouldEqual("RegisterInvoice");
    [Fact] void should_carry_the_argument_name() => _result.Arguments[0].Name.ShouldEqual("invoiceId");
    [Fact] void should_carry_the_argument_as_a_binding() => _result.Arguments[0].Value.Path.ShouldEqual("item.invoiceId");
    [Fact] void should_carry_the_success_continuation() => _result.OnSuccess[0].Kind.ShouldEqual(SceneModel.InteractionActionKind.CloseDialog);
    [Fact] void should_carry_the_failure_continuation() => _result.OnFailure[0].Kind.ShouldEqual(SceneModel.InteractionActionKind.Notify);
    [Fact] void should_have_no_result_continuation() => _result.OnResult.ShouldBeEmpty();
}
