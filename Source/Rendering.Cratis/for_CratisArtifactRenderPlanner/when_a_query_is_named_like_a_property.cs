// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.Http;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_query_is_named_like_a_property : Specification
{
    ArtifactRenderPlan _plan;

    void Because() => _plan = member_collision_model.Plan("Record", "Total");

    [Fact] void should_render_without_errors() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_report_the_exact_escape() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Information && diagnostic.Message.Contains("Read model 'PaymentTotals' query 'Total'", StringComparison.Ordinal) && diagnostic.Message.Contains("Total_", StringComparison.Ordinal));
    [Fact]
    void should_compile_with_the_original_arc_route()
    {
        Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
        var assembly = RenderedOutput.Load(member_collision_model.Files(_plan));
        var type = assembly.GetType("Payments.Payments.Recording.Lookup.PaymentTotals")!;
        var method = type.GetMethod("Total_")!;
        Assert.Equal(EndpointRouteHelper.BuildRouteUrl(Options(), type.Namespace!.Split('.'), GeneratedApiRoutes.SegmentsToSkipForRoute, "Total", true), method.GetCustomAttribute<PathAttribute>()!.Path);
    }

    [Fact]
    void should_share_the_scaffold_route_settings()
    {
        var settings = _plan.Artifacts.Single(artifact => artifact.RelativePath == "appsettings.json");
        using var document = JsonDocument.Parse(settings.Bytes.AsMemory());
        var endpoints = document.RootElement.GetProperty("Cratis").GetProperty("Arc").GetProperty("GeneratedApis");
        Assert.Equal(GeneratedApiRoutes.RoutePrefix, endpoints.GetProperty("RoutePrefix").GetString());
        Assert.Equal(GeneratedApiRoutes.SegmentsToSkipForRoute, endpoints.GetProperty("SegmentsToSkipForRoute").GetInt32());
    }

    [Theory]
    [InlineData("Payments.Accounting.HTTPReports.Lookup", "HTTP2Totals")]
    [InlineData("Payments.Payments.Recording.Lookup", "Total")]
    [InlineData("Payments.V2.Reports", "_1Total")]
    void should_match_arc_for_the_original_method(string @namespace, string method) =>
        Assert.Equal(EndpointRouteHelper.BuildRouteUrl(Options(), @namespace.Split('.'), GeneratedApiRoutes.SegmentsToSkipForRoute, method, true), GeneratedApiRoutes.QueryPath(@namespace, method));

    static ApiEndpointOptions Options() => new() { RoutePrefix = GeneratedApiRoutes.RoutePrefix, SegmentsToSkipForRoute = GeneratedApiRoutes.SegmentsToSkipForRoute };
}
#endif
