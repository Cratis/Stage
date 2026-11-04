// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;

/// <summary>
/// A clock fixed at one instant.
/// </summary>
/// <param name="now">The instant.</param>
public class FakeClock(DateTimeOffset now) : TimeProvider
{
    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => now;
}
