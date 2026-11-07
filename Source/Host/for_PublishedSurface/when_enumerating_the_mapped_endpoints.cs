// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Publishing;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.Published.for_PublishedSurface;

public class when_enumerating_the_mapped_endpoints
{
    static readonly PackageManifest _manifest = new(1, "App", "app", "1.0.0", "1.0.0", "pub-1", "sha256:0", false, new AuthenticationRequirements(true, [], [], [], false), []);

    static async Task<WebApplication> PublishedApp(Action<WebApplication>? extra = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();
        app.UseToolingRefusal();

        // What the published host maps itself. Tooling is mapped nowhere; this is the same set SemanticHost maps when published.
        SemanticHost.MapStatus(app, () => new StageStatus("ready", null, null));
        ModelReferencesEndpoint.Map(app, _manifest, false);
        app.MapGet("/api/orders", () => "ok");
        extra?.Invoke(app);
        app.MapFallback(() => Results.Text("page"));
        await app.StartAsync();

        return app;
    }

    static IReadOnlyList<string> Mapped(WebApplication app) => [.. ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)
        .OfType<RouteEndpoint>().Select(endpoint => endpoint.RoutePattern.RawText ?? string.Empty)];

    [Fact]
    async Task should_map_no_tooling_route()
    {
        await using var app = await PublishedApp();
        PublishedSurface.ToolingRoutes(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)).ShouldBeEmpty();
        Mapped(app).ShouldContain("/stage/model-references");
        PublishedSurface.EnsureNoTooling(app);
    }

    [Fact]
    async Task should_fail_when_a_tooling_route_appears()
    {
        await using var app = await PublishedApp(application =>
        {
            application.MapGet("/scalar/{documentName}", (string documentName) => documentName);
            application.MapPost("/stage/load", () => "load");
        });
        var refused = Assert.Throws<PublishedModeRefused>(() => PublishedSurface.EnsureNoTooling(app));
        refused.Message.ShouldContain("/scalar/{documentName}");
        refused.Message.ShouldContain("/stage/load");
    }

    [Theory]
    [InlineData("/scalar/v1")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/workbench")]
    [InlineData("/workbench/index.html")]
    [InlineData("/read-models")]
    [InlineData("/read-models/orders")]
    [InlineData("/stage/read-models")]
    [InlineData("/stage/load")]
    [InlineData("/stage/semantic/admission")]
    [InlineData("/stage/semantic/specifications/run")]
    [InlineData("/stage/packages")]
    [InlineData("/stage/packages/files/x.js")]
    public async Task should_answer_404_for_tooling(string path)
    {
        await using var app = await PublishedApp();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        (await client.GetAsync(path)).StatusCode.ShouldEqual(HttpStatusCode.NotFound);
        (await client.PostAsync(path, null)).StatusCode.ShouldEqual(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/stage/status")]
    [InlineData("/stage/model-references")]
    [InlineData("/api/orders")]
    [InlineData("/orders/42")]
    public async Task should_serve_the_application(string path)
    {
        await using var app = await PublishedApp();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.First()) };
        (await client.GetAsync(path)).StatusCode.ShouldEqual(HttpStatusCode.OK);
    }
}
