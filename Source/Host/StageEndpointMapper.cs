// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Queries;

namespace Cratis.Stage.Host;

/// <summary>
/// Adds admitted legacy aliases to Arc-produced endpoints without replacing its HTTP request pipeline.
/// </summary>
/// <param name="mapper">Arc's native endpoint mapper.</param>
/// <param name="surface">The surface admitted before resolving any modeled providers.</param>
internal sealed class StageEndpointMapper(IEndpointMapper mapper, StageHttpSurface surface) : IEndpointMapper
{
    /// <inheritdoc/>
    public bool EndpointExists(string name) => mapper.EndpointExists(name);

    /// <inheritdoc/>
    public void MapGet(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) =>
        MapMethod("GET", pattern, handler, metadata);

    /// <inheritdoc/>
    public void MapPost(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) =>
        MapMethod("POST", pattern, handler, metadata);

    /// <inheritdoc/>
    public void MapMethod(string httpMethod, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null)
    {
        mapper.MapMethod(httpMethod, pattern, handler, metadata);

        if (metadata is not null && surface.AliasFor(httpMethod, pattern) is { } alias)
        {
            // Keep the exact Arc delegate, body/response types, authorization and tags. Only discovery identity
            // changes: canonical operations remain the only advertised HTTP contract.
            var aliasMetadata = metadata with { Name = $"{metadata.Name}.StageLegacy", ExcludeFromApiDescription = true };
            if (!mapper.EndpointExists(aliasMetadata.Name))
            {
                mapper.MapMethod(httpMethod, alias, handler, aliasMetadata);
            }
        }
    }

    /// <summary>
    /// Pre-maps canonical names so the ordinary UseCratisArc mapping pass recognizes them as existing.
    /// </summary>
    /// <param name="app">The host application.</param>
    /// <param name="surface">The already admitted model surface.</param>
    internal static void Map(WebApplication app, StageHttpSurface surface)
    {
        var mapper = new StageEndpointMapper(new AspNetCoreEndpointMapper(app), surface);
        mapper.MapCommandEndpoints(app.Services);
        mapper.MapQueryEndpoints(app.Services);
    }
}
