// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// Tells which extension systems a screen places components of. The model says it, and the language does it once screens can use extension components (Cratis/Screenplay#1255).
/// </summary>
public interface IScreenExtensionUsage
{
    /// <summary>
    /// Finds the systems whose components a screen places.
    /// </summary>
    /// <param name="screen">The route of the screen.</param>
    /// <returns>The names of the systems, empty when the screen places none or is not known.</returns>
    IReadOnlyList<string> SystemsOn(string screen);
}

/// <summary>
/// The <see cref="IScreenExtensionUsage"/> of a model whose screens place no extension components, which is every model until screens can.
/// </summary>
public sealed class NoScreenExtensionUsage : IScreenExtensionUsage
{
    /// <inheritdoc/>
    public IReadOnlyList<string> SystemsOn(string screen) => [];
}

/// <summary>
/// The backend of a system as a screen found it.
/// </summary>
/// <param name="System">The name of the system.</param>
/// <param name="State">Where the backend is in its life.</param>
public sealed record ScreenBackend(string System, BackendState State);

/// <summary>
/// Starts the backends a screen needs when the browser reaches it, so they are running before the first call from one of the screen's components. Reaching a screen is the trigger, and
/// mounting a component is not: the screen's route is known before it renders, so the backend starts while it loads, and a component that mounts later, or conditionally, finds it ready or
/// starts it by its first call like any other.
/// </summary>
/// <param name="usage">The <see cref="IScreenExtensionUsage"/>.</param>
/// <param name="activator">The <see cref="ExtensionActivator"/>.</param>
public sealed class ScreenActivation(IScreenExtensionUsage usage, ExtensionActivator activator)
{
    /// <summary>
    /// Starts the backends of the systems a screen places components of, without waiting for them.
    /// </summary>
    /// <param name="screen">The route of the screen.</param>
    /// <returns>The systems and where their backends are now. A system with no backend is not listed.</returns>
    public IReadOnlyList<ScreenBackend> Reach(string screen) =>
        [.. usage.SystemsOn(screen).Where(activator.HasBackend).Distinct(StringComparer.OrdinalIgnoreCase).Select(system => new ScreenBackend(system, activator.ActivateInBackground(system, ActivationReason.Navigation)))];
}
