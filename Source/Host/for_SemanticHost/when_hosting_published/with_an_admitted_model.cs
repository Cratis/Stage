// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Specifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_published;

public class with_an_admitted_model : given.a_semantic_host
{
    void Establish() => _arguments = ["--published", "--Stage:Sandbox:ForwardedIdentity=true", "--Stage:Sandbox:Identity=true"];
    DefaultHttpContext _journeyReplay = null!;

    async Task Because()
    {
        await BuildHost();
        _journeyReplay = new DefaultHttpContext { RequestServices = _app.Services };
        _journeyReplay.Request.Method = "POST";
        _journeyReplay.Request.Path = StageJourneyReplay.Route;
        await ((IApplicationBuilder)_app).Build()(_journeyReplay);
    }

    [Fact] void should_admit_the_model() => _refused.ShouldBeFalse();
    [Fact] void should_only_map_the_runtime_stage_routes() => _patterns.Where(pattern => pattern.StartsWith("/stage/", StringComparison.Ordinal)).ShouldContainOnly(["/stage/status", "/stage/packages", "/stage/scene", "/stage/routes", "/stage/locales", "/stage/strings/{locale}"]);
    [Fact] void should_not_map_any_tooling() => Published.PublishedSurface.ToolingRoutes(((Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)_app).DataSources.SelectMany(source => source.Endpoints)).ShouldBeEmpty();
    [Fact] void should_not_map_the_journey_replay() => _patterns.ShouldNotContain(StageJourneyReplay.Route);
    [Fact] void should_refuse_a_journey_replay() => _journeyReplay.Response.StatusCode.ShouldEqual(StatusCodes.Status404NotFound);
    [Fact] void should_not_map_the_read_model_page() => _patterns.ShouldNotContain("/read-models");
    [Fact] void should_not_redirect_to_the_read_model_page() => _readModelPage.Response.StatusCode.ShouldEqual(StatusCodes.Status404NotFound);
    [Fact] void should_not_map_openapi() => _patterns.Any(pattern => pattern.Contains("openapi", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_not_map_scalar() => _patterns.Any(pattern => pattern.Contains("scalar", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_not_map_workbench() => _patterns.Any(pattern => pattern.StartsWith("/workbench", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_application_apis() => _patterns.Any(pattern => pattern.StartsWith("/api/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_arc_discovery() => _patterns.Any(pattern => pattern.StartsWith("/.cratis/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_spa_fallback() => _patterns.ShouldContain("{*path:nonfile}");
    [Fact] void should_disable_personas_even_when_explicitly_enabled() => _app.Services.GetRequiredService<SandboxIdentityOptions>().Enabled.ShouldBeFalse();
    [Fact] void should_trust_the_forwarded_principal() => _app.Services.GetRequiredService<IOptions<ArcOptions>>().Value.TrustForwardedIdentityHeaders.ShouldBeTrue();
}
