// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Outlets;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Builds the outlet chain of a translated screen with Scene's chain builder, adding the slice level Stage translates.
/// </summary>
/// <remarks>
/// <see cref="OutletChains.Of"/> builds the chain from the document, the navigation model and the host's levels for modules and features.
/// A slice screen's own refinements (<see cref="OutletLevelKind.Slice"/>) are not a module or feature of the screen's path, so they are kept
/// out of the host's levels and put just above the screen's own level - after every feature, before the screen.
/// </remarks>
public static class StageOutletChains
{
    /// <summary>
    /// Builds the outlet chain of a screen.
    /// </summary>
    /// <param name="application">The translated <see cref="SceneApplication"/>.</param>
    /// <param name="screen">The name of the screen.</param>
    /// <returns>The <see cref="OutletChain"/>, empty when the application has no such screen.</returns>
    public static OutletChain Of(SceneApplication application, string screen)
    {
        var all = application.OutletLevels ?? [];
        var levels = all.Where(level => level.Kind != OutletLevelKind.Slice).ToList();
        var chain = OutletChains.Of(
            new OutletChainSource(application.Layouts, application.ScreenTemplates, application.Screens, application.Navigation, levels),
            screen);
        var slice = all.FirstOrDefault(level => level.Kind == OutletLevelKind.Slice && level.Name == screen);
        if (slice is null || chain.Chain.Count == 0) return chain;

        return chain with { Chain = [.. chain.Chain.Take(chain.Chain.Count - 1), slice, chain.Chain[^1]] };
    }

    /// <summary>
    /// Resolves what an outlet of a screen shows, through Scene's engine.
    /// </summary>
    /// <param name="application">The translated <see cref="SceneApplication"/>.</param>
    /// <param name="screen">The name of the screen.</param>
    /// <param name="outlet">The name of the outlet.</param>
    /// <returns>The <see cref="EffectiveOutlet"/>.</returns>
    public static EffectiveOutlet Resolve(SceneApplication application, string screen, string outlet)
    {
        var chain = Of(application, screen);
        return EffectiveOutlets.Resolve(chain.Chain, outlet, chain.Contributors);
    }
}
