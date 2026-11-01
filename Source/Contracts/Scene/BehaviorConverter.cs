// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
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
    public static SceneModel.Behavior Convert(ScreenplaySyntax.BehaviorSyntax behavior) => Convert(behavior, null);

    /// <summary>
    /// Converts a <see cref="ScreenplaySyntax.BehaviorSyntax"/> into a <see cref="SceneModel.Behavior"/>, typing its guards.
    /// </summary>
    /// <param name="behavior">The <see cref="ScreenplaySyntax.BehaviorSyntax"/> to convert.</param>
    /// <param name="enumerationOf">Gives the members of an enumeration-typed guard field.</param>
    /// <returns>The converted <see cref="SceneModel.Behavior"/>.</returns>
    public static SceneModel.Behavior Convert(ScreenplaySyntax.BehaviorSyntax behavior, Func<string, IReadOnlyList<string>?>? enumerationOf) =>
        Convert(behavior, enumerationOf, null, behavior.Name ?? string.Empty);

    /// <summary>
    /// Converts a behavior while collecting omitted interaction constructs.
    /// </summary>
    /// <param name="behavior">The behavior to convert.</param>
    /// <param name="enumerationOf">The enumeration members for guard fields.</param>
    /// <param name="diagnostics">The collector, or null for legacy throwing behavior.</param>
    /// <param name="path">The owning attachment path.</param>
    /// <returns>The behavior with supported bindings and actions.</returns>
    public static SceneModel.Behavior Convert(ScreenplaySyntax.BehaviorSyntax behavior, Func<string, IReadOnlyList<string>?>? enumerationOf, ConversionDiagnostics? diagnostics, string path) =>
        Convert(behavior, enumerationOf, diagnostics, path, null);

    /// <summary>Converts a behavior using declared guard field types.</summary>
    /// <param name="behavior">The behavior to convert.</param>
    /// <param name="enumerationOf">The guard enumeration resolver.</param>
    /// <param name="diagnostics">The conversion diagnostics.</param>
    /// <param name="path">The attachment path.</param>
    /// <param name="isStringField">The known string field resolver.</param>
    /// <returns>The converted behavior.</returns>
    public static SceneModel.Behavior Convert(ScreenplaySyntax.BehaviorSyntax behavior, Func<string, IReadOnlyList<string>?>? enumerationOf, ConversionDiagnostics? diagnostics, string path, Func<string, bool>? isStringField) =>
        new(behavior.Name, [.. behavior.Bindings.SelectMany(binding => ConvertBinding(binding, enumerationOf, diagnostics, path, isStringField))], behavior.Order);

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

    /// <summary>
    /// Converts a trigger, reporting unsupported triggers, kinds and interval units.
    /// </summary>
    /// <param name="trigger">The authored trigger.</param>
    /// <param name="diagnostics">The collector, or null for legacy throwing behavior.</param>
    /// <param name="path">The owning interaction path.</param>
    /// <returns>The trigger, or null when unsupported.</returns>
    public static SceneModel.InteractionTrigger? ConvertTrigger(ScreenplaySyntax.InteractionTriggerSyntax trigger, ConversionDiagnostics? diagnostics, string path)
    {
        if (diagnostics is null) return ConvertTrigger(trigger);
        var construct = trigger switch
        {
            ScreenplaySyntax.BuiltInInteractionTriggerSyntax builtIn when builtIn.Kind is not (ScreenplaySyntax.InteractionTriggerKind.Click or ScreenplaySyntax.InteractionTriggerKind.DoubleClick or ScreenplaySyntax.InteractionTriggerKind.Select or ScreenplaySyntax.InteractionTriggerKind.Submit or ScreenplaySyntax.InteractionTriggerKind.Change or ScreenplaySyntax.InteractionTriggerKind.Load or ScreenplaySyntax.InteractionTriggerKind.Unload or ScreenplaySyntax.InteractionTriggerKind.Enter or ScreenplaySyntax.InteractionTriggerKind.Leave) => $"interaction trigger kind {builtIn.Kind}",
            ScreenplaySyntax.IntervalInteractionTriggerSyntax interval when interval.Unit is not (ScreenplaySyntax.IntervalUnit.Seconds or ScreenplaySyntax.IntervalUnit.Minutes or ScreenplaySyntax.IntervalUnit.Hours or ScreenplaySyntax.IntervalUnit.Days) => $"interval unit {interval.Unit}",
            ScreenplaySyntax.BuiltInInteractionTriggerSyntax or ScreenplaySyntax.EventInteractionTriggerSyntax or
                ScreenplaySyntax.IntervalInteractionTriggerSyntax or ScreenplaySyntax.ApplicationTriggerInteractionTriggerSyntax => null,
            _ => $"interaction trigger {trigger.GetType().Name}"
        };
        if (construct is null) return ConvertTrigger(trigger);
        var code = trigger is ScreenplaySyntax.IntervalInteractionTriggerSyntax ? "STAGE-SCENE-BEHAVIOR-002" : "STAGE-SCENE-BEHAVIOR-001";
        diagnostics.Report(new(code, DiagnosticSeverity.Error, construct, path, trigger.Location, $"Unsupported {construct} was omitted from '{path}'."));
        return null;
    }

    // Scene owns ordered first-match selection for guarded interaction alternatives.
    static IEnumerable<SceneModel.InteractionBinding> ConvertBinding(ScreenplaySyntax.InteractionBindingSyntax binding, Func<string, IReadOnlyList<string>?>? enumerationOf, ConversionDiagnostics? diagnostics, string path, Func<string, bool>? isStringField)
    {
        var trigger = ConvertTrigger(binding.Trigger, diagnostics, path);
        if (trigger is null) yield break;
        var alternatives = binding.Alternatives.ToArray();
        if (alternatives.Length == 0 && binding.Otherwise is null)
        {
            yield return new(
                trigger,
                [.. binding.Actions.Select(action => InteractionActionConverter.Convert(action, diagnostics, path)).OfType<SceneModel.InteractionAction>()],
                binding.Condition is null ? null : new SceneCommon.BindingExpression(binding.Condition));
            yield break;
        }

        yield return new(
            trigger,
            [],
            Alternatives: [.. alternatives.Select(alternative => new SceneModel.InteractionAlternative(
                SceneGuards.ConvertToCondition(alternative.Condition, enumerationOf, isStringField),
                [.. alternative.Actions.Select(action => InteractionActionConverter.Convert(action, diagnostics, path)).OfType<SceneModel.InteractionAction>()]))],
            Otherwise: binding.Otherwise is null ? null : [.. binding.Otherwise.Actions.Select(action => InteractionActionConverter.Convert(action, diagnostics, path)).OfType<SceneModel.InteractionAction>()]);
    }

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
