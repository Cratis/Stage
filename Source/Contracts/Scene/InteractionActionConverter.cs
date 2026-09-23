// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneCommon = Cratis.Scene.Model.Common;
using SceneModel = Cratis.Scene.Model.Interactions;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay interaction action into a <see cref="SceneModel.InteractionAction"/>.
/// </summary>
/// <remarks>
/// Every action the grammar admits has a home here. An action kind with no case is a translation gap rather
/// than something to pass through, so it throws <see cref="UnknownInteractionAction"/> - the same posture the
/// other converters in this folder take.
/// </remarks>
public static class InteractionActionConverter
{
    /// <summary>
    /// The prefix marking a message as a reference into the string table rather than a literal.
    /// </summary>
    public const string StringsPrefix = "$strings.";

    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.InteractionActionSyntax"/> into a <see cref="SceneModel.InteractionAction"/>.
    /// </summary>
    /// <param name="action">The <see cref="ScreenplaySyntax.InteractionActionSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneModel.InteractionAction"/>.</returns>
    public static SceneModel.InteractionAction Convert(ScreenplaySyntax.InteractionActionSyntax action)
    {
        var converted = ConvertCore(action);

        // The continuations live on the base, so they are carried once here rather than in each case. An action
        // that cannot fail simply has none of them.
        return converted with
        {
            Arguments = [.. action.Arguments.Select(ConvertArgument)],
            OnSuccess = [.. action.OnSuccess.Select(Convert)],
            OnFailure = [.. action.OnFailure.Select(Convert)],
            OnResult = [.. action.OnResult.Select(Convert)]
        };
    }

    /// <summary>
    /// Converts an authored message into an <see cref="SceneModel.InteractionMessage"/>.
    /// </summary>
    /// <param name="message">The message as authored.</param>
    /// <param name="isLiteral">Whether it was written as a string literal.</param>
    /// <returns>The converted <see cref="SceneModel.InteractionMessage"/>.</returns>
    /// <remarks>
    /// The three forms stay distinguishable all the way to the renderer: a literal is shown as written, a
    /// <c>$strings.</c> reference is looked up in the string table, and anything else is a binding the
    /// surrounding scope supplies. Collapsing them into one string would make a missing translation
    /// indistinguishable from a deliberate literal.
    /// </remarks>
    public static SceneModel.InteractionMessage ConvertMessage(string message, bool isLiteral)
    {
        if (isLiteral) return new(Text: message);

        return message.StartsWith(StringsPrefix, StringComparison.Ordinal)
            ? new(StringsKey: message[StringsPrefix.Length..])
            : new(Binding: new SceneCommon.BindingExpression(message));
    }

    static SceneModel.InteractionAction ConvertCore(ScreenplaySyntax.InteractionActionSyntax action) =>
        action switch
        {
            ScreenplaySyntax.ExecuteCommandActionSyntax execute => new SceneModel.ExecuteCommandAction(execute.Command),
            ScreenplaySyntax.NavigateActionSyntax navigate => new SceneModel.NavigateAction(navigate.Screen),
            ScreenplaySyntax.NavigateBackActionSyntax => new SceneModel.NavigateBackAction(),
            ScreenplaySyntax.OpenDialogActionSyntax openDialog => new SceneModel.OpenDialogAction(openDialog.DialogTemplate),
            ScreenplaySyntax.CloseDialogActionSyntax => new SceneModel.CloseDialogAction(),
            ScreenplaySyntax.RefreshQueryActionSyntax refresh => new SceneModel.RefreshQueryAction(refresh.Query),
            ScreenplaySyntax.SetStateActionSyntax setState => new SceneModel.SetStateAction(
                setState.Target,
                new SceneCommon.BindingExpression(setState.Value)),
            ScreenplaySyntax.NotifyActionSyntax notify => new SceneModel.NotifyAction(
                ConvertLevel(notify.Level),
                ConvertMessage(notify.Message, notify.MessageIsLiteral)),
            ScreenplaySyntax.ConfirmActionSyntax confirm => new SceneModel.ConfirmAction(
                ConvertMessage(confirm.Message, confirm.MessageIsLiteral)),
            ScreenplaySyntax.RaiseTriggerActionSyntax raise => new SceneModel.RaiseTriggerAction(raise.Trigger),
            _ => throw new UnknownInteractionAction(action.GetType().Name),
        };

    static SceneModel.InteractionArgument ConvertArgument(ScreenplaySyntax.InteractionArgumentSyntax argument) =>
        new(argument.Name, new SceneCommon.BindingExpression(argument.Binding));

    static SceneModel.NotificationLevel ConvertLevel(ScreenplaySyntax.NotificationLevel level) =>
        level switch
        {
            ScreenplaySyntax.NotificationLevel.Info => SceneModel.NotificationLevel.Info,
            ScreenplaySyntax.NotificationLevel.Warning => SceneModel.NotificationLevel.Warning,
            ScreenplaySyntax.NotificationLevel.Error => SceneModel.NotificationLevel.Error,
            _ => throw new UnknownNotificationLevel(level.ToString()),
        };
}
