// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SceneLayouts = Cratis.Scene.Model.Layouts;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// The arrangement one slot-bearing structure resolves to at a <see cref="RenderPlan"/>'s size class - part of
/// Cratis/Stage#39.
/// </summary>
/// <remarks>
/// Exactly one of <paramref name="Flow"/>, <paramref name="Slots"/> and <paramref name="Elements"/> is set,
/// matching the three arrangement shapes <c>Cratis.Scene.Model</c> defines. They are Scene's own evaluated
/// results, carried through unchanged - the selection is what Scene's evaluators returned, not a Stage-side
/// interpretation of them.
/// </remarks>
/// <param name="Structure">The name of the layout, screen template or dialog template the arrangement belongs to.</param>
/// <param name="Slot">The name of the slot whose own arrangement this is, or <see langword="null"/> for the structure's own macro arrangement over its slots.</param>
/// <param name="Flow">The flow tree that applies, for a <see cref="SceneLayouts.FlowArrangement"/>.</param>
/// <param name="Slots">The slot placement variant that applies, for a <see cref="SceneLayouts.FreeformSlotArrangement"/>.</param>
/// <param name="Elements">The element placement variant that applies, for a <see cref="SceneLayouts.FreeformArrangement"/>.</param>
public record ArrangementSelection(
    string Structure,
    string? Slot,
    SceneLayouts.FlowNode? Flow = null,
    SceneLayouts.FreeformSlotVariant? Slots = null,
    SceneLayouts.FreeformVariant? Elements = null);
