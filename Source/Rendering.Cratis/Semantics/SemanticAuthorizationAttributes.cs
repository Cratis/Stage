// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Chooses the authorization attributes for admitted commands and queries.
/// </summary>
internal static class SemanticAuthorizationAttributes
{
    /// <summary>
    /// Gets the authorization attribute for an admitted command.
    /// </summary>
    /// <param name="command">The admitted command.</param>
    /// <returns>The attribute name.</returns>
    public static string For(SemanticCommand command) => command.Authorization is null
        ? "AllowAnonymous"
        : $"Authorize(Policy = \"{Policies.SemanticPolicyArtifactRenderer.Name(command.Id)}\")";

    /// <summary>
    /// Gets the authorization attribute for an admitted query.
    /// </summary>
    /// <param name="query">The admitted query.</param>
    /// <returns>The attribute name.</returns>
    public static string For(SemanticKeyedQuery query) => query.Authorization is null
        ? "AllowAnonymous"
        : $"Authorize(Policy = \"{Policies.SemanticPolicyArtifactRenderer.Name(query.Id)}\")";
}
