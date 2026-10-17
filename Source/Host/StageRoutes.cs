// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Semantics;

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

    /// <summary>
    /// Gets every modeled command's presentation schema, used to register native command proxies in the live host.
    /// </summary>
    public IReadOnlyDictionary<string, string> CommandSchemas { get; init; } = new Dictionary<string, string>();

    internal static StageRoutes For(StageSceneRoutes routes, EventModel model) => new(routes.CommandRoutes, routes.QueryRoutes)
    {
        Diagnostics = routes.Diagnostics,
        CommandSchemas = StageModelWalker.Slices(model)
            .Where(located => located.Slice.Command is not null)
            .GroupBy(located => located.Slice.Command!.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Slice.Command!.Schema, StringComparer.Ordinal)
    };

    internal static StageRoutes For(StageSceneRoutes routes, ExecutableSemanticModel model) => new(routes.CommandRoutes, routes.QueryRoutes)
    {
        Diagnostics = routes.Diagnostics,
        CommandSchemas = SemanticModelWalker.Slices(model)
            .SelectMany(located => located.Slice.Commands)
            .GroupBy(command => command.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => CommandFormSchemas.For(model.Application, group.First().Properties), StringComparer.Ordinal)
    };
}
