// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Semantics;

/// <summary>
/// Tells a subscriber when the committed semantic world of a runtime has changed.
/// </summary>
/// <remarks>
/// A callback runs on the thread that committed the change, while the runtime still holds its gate. It must
/// therefore only schedule work and return; reading the runtime from inside the callback would wait for itself.
/// </remarks>
internal interface ISemanticWorldChanges
{
    /// <summary>
    /// Subscribes to every committed change of the world.
    /// </summary>
    /// <param name="changed">The callback to run after the world changed.</param>
    /// <returns>A handle that ends the subscription when disposed.</returns>
    IDisposable Subscribe(Action changed);
}
