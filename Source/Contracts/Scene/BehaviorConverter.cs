// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneCommon = Cratis.Scene.Model.Common;
using SceneModel = Cratis.Scene.Model.Interactions;
using ScreenplaySyntax = Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Converts a compiled Screenplay <see cref="ScreenplaySyntax.BehaviorSyntax"/> into a
/// <see cref="SceneModel.Behavior"/> - part of Cratis/Stage#37.
/// </summary>
/// <remarks>
/// A named behavior and an inline <c language="csharp">on</c> block are the same construct on both sides, so there is one
/// conversion rather than two: anonymity survives as a null name.
/// <para>
/// A behavior's parameters are deliberately not carried. They are a declaration-site concern that a
/// <c language="csharp">uses</c> site resolves at attachment time, and the model records what will run, not how it was
/// parameterized. Resolving them is Plan 05's work, once <c language="csharp">accepts</c> and <c language="csharp">state</c> give the
/// argument expressions a scope to be checked against.
/// </para>
/// </remarks>
public static class BehaviorConverter
{
    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.BehaviorSyntax"/> into a <see cref="SceneModel.Behavior"/>.
    /// </summary>
    /// <param name="behavior">The <see cref="ScreenplaySyntax.BehaviorSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneModel.Behavior"/>.</returns>
    public static SceneModel.Behavior Convert(ScreenplaySyntax.BehaviorSyntax behavior) =>
        new(behavior.Name, [.. behavior.Bindings.Select(ConvertBinding)], behavior.Order);

    /// <summary>
    /// Converts a sequence of <see cref="ScreenplaySyntax.BehaviorSyntax"/> into Scene behaviors.
    /// </summary>
    /// <param name="behaviors">The behaviors to convert.</param>
    /// <returns>The converted behaviors.</returns>
    public static IReadOnlyList<SceneModel.Behavior> ConvertAll(IEnumerable<ScreenplaySyntax.BehaviorSyntax> behaviors) =>
        [.. behaviors.Select(Convert)];

    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.InteractionTriggerSyntax"/> into a <see cref="SceneModel.InteractionTrigger"/>.
    /// </summary>
    /// <param name="trigger">The <see cref="ScreenplaySyntax.InteractionTriggerSyntax"/> to convert.</param>
    /// <returns>The converted <see cref="SceneModel.InteractionTrigger"/>.</returns>
    public static SceneModel.InteractionTrigger ConvertTrigger(ScreenplaySyntax.InteractionTriggerSyntax trigger) =>
        trigger switch
        {
            ScreenplaySyntax.BuiltInInteractionTriggerSyntax builtIn => new SceneModel.BuiltInInteractionTrigger(ConvertKind(builtIn.Kind)),
            ScreenplaySyntax.EventInteractionTriggerSyntax @event => new SceneModel.EventInteractionTrigger(@event.EventName),
            ScreenplaySyntax.IntervalInteractionTriggerSyntax interval => new SceneModel.IntervalInteractionTrigger(ToSeconds(interval)),
            ScreenplaySyntax.ApplicationTriggerInteractionTriggerSyntax application => new SceneModel.ApplicationTriggerInteractionTrigger(application.TriggerName),
            _ => throw new UnknownInteractionTrigger(trigger.GetType().Name),
        };

    static SceneModel.InteractionBinding ConvertBinding(ScreenplaySyntax.InteractionBindingSyntax binding) =>
        new(
            ConvertTrigger(binding.Trigger),
            [.. binding.Actions.Select(InteractionActionConverter.Convert)],
            binding.Condition is null ? null : new SceneCommon.BindingExpression(binding.Condition));

    /// <summary>
    /// Normalizes an interval to seconds, which is the one unit the model carries.
    /// </summary>
    /// <remarks>
    /// Screenplay lets an interval be authored in whichever unit reads best, but a renderer scheduling a timer
    /// should not have to know about units. Converting here keeps that choice an authoring convenience rather
    /// than something every consumer re-implements.
    /// </remarks>
    static int ToSeconds(ScreenplaySyntax.IntervalInteractionTriggerSyntax interval) =>
        interval.Unit switch
        {
            ScreenplaySyntax.IntervalUnit.Seconds => interval.Amount,
            ScreenplaySyntax.IntervalUnit.Minutes => interval.Amount * 60,
            ScreenplaySyntax.IntervalUnit.Hours => interval.Amount * 60 * 60,
            ScreenplaySyntax.IntervalUnit.Days => interval.Amount * 60 * 60 * 24,
            _ => throw new UnknownIntervalUnit(interval.Unit.ToString()),
        };

    static SceneModel.InteractionTriggerKind ConvertKind(ScreenplaySyntax.InteractionTriggerKind kind) =>
        kind switch
        {
            ScreenplaySyntax.InteractionTriggerKind.Click => SceneModel.InteractionTriggerKind.Click,
            ScreenplaySyntax.InteractionTriggerKind.DoubleClick => SceneModel.InteractionTriggerKind.DoubleClick,
            ScreenplaySyntax.InteractionTriggerKind.Select => SceneModel.InteractionTriggerKind.Select,
            ScreenplaySyntax.InteractionTriggerKind.Submit => SceneModel.InteractionTriggerKind.Submit,
            ScreenplaySyntax.InteractionTriggerKind.Change => SceneModel.InteractionTriggerKind.Change,
            ScreenplaySyntax.InteractionTriggerKind.Load => SceneModel.InteractionTriggerKind.Load,
            ScreenplaySyntax.InteractionTriggerKind.Unload => SceneModel.InteractionTriggerKind.Unload,
            ScreenplaySyntax.InteractionTriggerKind.Enter => SceneModel.InteractionTriggerKind.Enter,
            ScreenplaySyntax.InteractionTriggerKind.Leave => SceneModel.InteractionTriggerKind.Leave,
            _ => throw new UnknownInteractionTrigger(kind.ToString()),
        };
}
