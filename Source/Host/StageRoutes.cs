// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host;

/// <summary>
/// Where the modeled commands and queries live in the running application.
/// </summary>
/// <param name="Commands">The route each command is posted to, by command name.</param>
/// <param name="Queries">The route each query is read from, by query name.</param>
public record StageRoutes(
    IReadOnlyDictionary<string, string> Commands,
    IReadOnlyDictionary<string, string> Queries)
{
    /// <summary>
    /// Gets every name left out of <see cref="Commands"/> or <see cref="Queries"/> because several distinct
    /// operations answer to it.
    /// </summary>
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
