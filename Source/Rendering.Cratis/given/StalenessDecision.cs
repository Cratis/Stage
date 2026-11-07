// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.given;

/// <summary>
/// Whether a labelled container is stale, and why.
/// </summary>
/// <param name="Reap">True when the container may be reaped.</param>
/// <param name="Reason">The reason, for the reaping report.</param>
public sealed record StalenessDecision(bool Reap, string Reason);
