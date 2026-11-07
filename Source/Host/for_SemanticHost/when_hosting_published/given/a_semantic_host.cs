// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Routing;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_published.given;

public class a_semantic_host : for_SemanticHost.given.a_model_path
{
    protected WebApplication _app = null!;
    protected string[] _arguments = [];
    protected string[] _patterns = [];
    protected bool _refused;
    protected DefaultHttpContext _readModelPage = null!;

    async Task Establish() => await File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source);

    private protected async Task BuildHost(Published.PublishedPackage? package = null, Published.PublishedOptions? published = null, StageHostMode? hostMode = null)
    {
        (_app, var initialize) = await SemanticHost.Build(_arguments, _path, package, published, hostMode);
        _refused = initialize is null;
        _patterns = [.. ((IEndpointRouteBuilder)_app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().Select(endpoint => endpoint.RoutePattern.RawText!)];
        _readModelPage = new DefaultHttpContext { RequestServices = _app.Services };
        _readModelPage.Request.Method = "GET";
        _readModelPage.Request.Path = "/read-models/";
        _app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        });
        await ((IApplicationBuilder)_app).Build()(_readModelPage);
    }

    async Task Destroy() => await _app.DisposeAsync();
}
