// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_sandbox;

public class with_a_refused_specification : when_hosting_published.given.a_semantic_host
{
    string _routes = null!;
    string _status = null!;
    SemanticModelLoadResult _strictFailure = null!;

    async Task Because()
    {
        await File.WriteAllTextAsync(_path, given.a_refused_specification.Source);
        _strictFailure = await SemanticModelLoader.LoadAsync(_directory, [_path], null, "Projects");
        await BuildHost();
        _routes = await Get("/stage/routes");
        _status = await Get("/stage/status");
    }

    async Task<string> Get(string path)
    {
        await using var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = _app.Services };
        context.Request.Method = "GET";
        context.Request.Path = path;
        context.Response.Body = body;
        var endpoint = ((IEndpointRouteBuilder)_app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().Single(endpoint => endpoint.RoutePattern.RawText == path);
        await endpoint.RequestDelegate!(context);
        context.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
        return System.Text.Encoding.UTF8.GetString(body.ToArray());
    }

    [Fact] void should_admit_the_running_model() => _refused.ShouldBeFalse();
    [Fact] void should_serve_the_healthy_command() => _routes.ShouldContain("RegisterProject");
    [Fact] void should_serve_the_healthy_query() => _patterns.Any(pattern => pattern.EndsWith("/project-by-id", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_map_the_blanket_api_refusal() => _patterns.ShouldNotContain("/api/{**path}");
    [Fact] void should_map_application_endpoints() => _patterns.Any(pattern => pattern.StartsWith("/api/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_strict_shared_loading() => _strictFailure.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact]
    void should_report_the_original_error_in_status()
    {
        using var document = JsonDocument.Parse(_status);
        var original = _strictFailure.Diagnostics.Single(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Source is not null);
        var note = document.RootElement.GetProperty("notes").EnumerateArray().Single(diagnostic => diagnostic.GetProperty("code").GetString() == original.Code);
        note.GetProperty("message").GetString().ShouldEqual(original.Message);
        note.GetProperty("severity").GetInt32().ShouldEqual((int)original.Severity);
        note.GetProperty("source").GetString().ShouldEqual(original.Source);
    }
}
