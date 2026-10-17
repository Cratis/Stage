// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Layouts;
using SceneLayouts = Cratis.Scene.Model.Layouts;
using SceneSizeClasses = Cratis.Scene.Model.SizeClasses;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Selects what one <see cref="SceneLayouts.Arrangement"/> resolves to at a given
/// <see cref="SceneSizeClasses.SizeClass"/> - part of Cratis/Stage#39.
/// </summary>
/// <remarks>
/// Every decision here belongs to Scene: <see cref="FlowArrangementEvaluator"/>,
/// <see cref="FreeformSlotArrangementEvaluator"/> and <see cref="FreeformArrangementEvaluator"/> pick the
/// applicable tree or variant, and this only dispatches on the arrangement's shape and labels the result. No
/// breakpoint, specificity or variant-matching rule is implemented here - a Stage-side one would be a second
/// answer to a question Scene already answers for every renderer, and the two would drift.
/// </remarks>
public static class ArrangementSelector
{
    /// <summary>
    /// Identifies an arrangement type that cannot be selected.
    /// </summary>
    public const string DiagnosticCode = "STAGE-SCENE-ARRANGEMENT-003";

    /// <summary>
    /// Tries to select an arrangement without throwing for an unknown arrangement type.
    /// </summary>
    /// <param name="structure">The owning structure.</param>
    /// <param name="slot">The owning slot, or null for a macro arrangement.</param>
    /// <param name="arrangement">The arrangement to select.</param>
    /// <param name="sizeClass">The target size class.</param>
    /// <param name="selection">The selection, or null for a missing freeform variant or unsupported type.</param>
    /// <returns>False only for an unsupported arrangement type; a missing freeform variant returns true.</returns>
    public static bool TrySelect(
        string structure,
        string? slot,
        SceneLayouts.Arrangement arrangement,
        SceneSizeClasses.SizeClass sizeClass,
        out ArrangementSelection? selection)
    {
        selection = null;
        if (arrangement is not (SceneLayouts.FlowArrangement or SceneLayouts.FreeformSlotArrangement or SceneLayouts.FreeformArrangement)) return false;
        selection = Select(structure, slot, arrangement, sizeClass);
        return true;
    }

    /// <summary>
    /// Selects the arrangement that applies at a size class.
    /// </summary>
    /// <param name="structure">The name of the layout, screen template or dialog template the arrangement belongs to.</param>
    /// <param name="slot">The name of the slot whose own arrangement this is, or <see langword="null"/> for the structure's own macro arrangement.</param>
    /// <param name="arrangement">The <see cref="SceneLayouts.Arrangement"/> to select from.</param>
    /// <param name="sizeClass">The <see cref="SceneSizeClasses.SizeClass"/> the target renders at.</param>
    /// <returns>
    /// The <see cref="ArrangementSelection"/>, or <see langword="null"/> when a freeform arrangement declares
    /// no variant for <paramref name="sizeClass"/>. Freeform has no fallback variant by design, so the caller
    /// reports the absence rather than substituting one.
    /// </returns>
    public static ArrangementSelection? Select(
        string structure,
        string? slot,
        SceneLayouts.Arrangement arrangement,
        SceneSizeClasses.SizeClass sizeClass) =>
        arrangement switch
        {
            SceneLayouts.FlowArrangement flow => new(structure, slot, Flow: FlowArrangementEvaluator.Evaluate(flow, sizeClass)),
            SceneLayouts.FreeformSlotArrangement slots => FreeformSlotArrangementEvaluator.Evaluate(slots, sizeClass) is { } variant
                ? new ArrangementSelection(structure, slot, Slots: variant)
                : null,
            SceneLayouts.FreeformArrangement elements => FreeformArrangementEvaluator.Evaluate(elements, sizeClass) is { } variant
                ? new ArrangementSelection(structure, slot, Elements: variant)
                : null,
            _ => throw new UnknownArrangement(arrangement.GetType().Name),
        };
}
