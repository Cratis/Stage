// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Extensions.Activation;

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// An <see cref="IScreenExtensionUsage"/> that knows, from the compiled model, which systems each screen places components of.
/// </summary>
/// <param name="systems">The systems each screen needs, by screen name.</param>
public sealed class ModelScreenExtensionUsage(IReadOnlyDictionary<string, IReadOnlyList<string>> systems) : IScreenExtensionUsage
{
    /// <inheritdoc/>
    public IReadOnlyList<string> SystemsOn(string screen) =>
        systems.TryGetValue(screen.Trim('/'), out var needed) ? needed : [];
}
