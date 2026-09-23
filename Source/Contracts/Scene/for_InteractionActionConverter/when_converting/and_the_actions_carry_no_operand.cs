// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;
using SceneModel = Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene.for_InteractionActionConverter.when_converting;

/// <summary>
/// Nothing about the shape of these two distinguishes them - both carry no operand. The discriminator is the
/// only thing that keeps them apart once translated, which is precisely why the model has one.
/// </summary>
public class and_the_actions_carry_no_operand : Specification
{
    SceneModel.InteractionAction _back = null!;
    SceneModel.InteractionAction _closeDialog = null!;

    void Because()
    {
        _back = InteractionActionConverter.Convert(new NavigateBackActionSyntax(SourceLocation.Start));
        _closeDialog = InteractionActionConverter.Convert(new CloseDialogActionSyntax(SourceLocation.Start));
    }

    [Fact] void should_identify_navigating_back() => _back.Kind.ShouldEqual(SceneModel.InteractionActionKind.NavigateBack);
    [Fact] void should_identify_closing_the_dialog() => _closeDialog.Kind.ShouldEqual(SceneModel.InteractionActionKind.CloseDialog);
    [Fact] void should_tell_them_apart() => _back.Kind.ShouldNotEqual(_closeDialog.Kind);
}
