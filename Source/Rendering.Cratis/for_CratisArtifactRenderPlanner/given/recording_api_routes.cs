// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Arc.Http;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal sealed class recording_api_routes : IEndpointMapper
{
    internal List<(string Method, string Route)> Routes { get; } = [];

    public void MapGet(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) => MapMethod("GET", pattern, handler, metadata);

    public void MapPost(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) => MapMethod("POST", pattern, handler, metadata);

    public void MapMethod(string httpMethod, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) => Routes.Add((httpMethod, pattern));

    public bool EndpointExists(string name) => false;
}
#endif
