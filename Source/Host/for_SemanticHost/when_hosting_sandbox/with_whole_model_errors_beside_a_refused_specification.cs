// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost.when_hosting_sandbox;

public class with_whole_model_errors_beside_a_refused_specification : when_hosting_published.given.a_semantic_host
{
    async Task Because()
    {
        await File.WriteAllTextAsync(_path, given.a_refused_specification.Source.Replace("produces ProjectRegistered", "produces MissingEvent", StringComparison.Ordinal));
        await BuildHost();
    }

    [Fact] void should_refuse_the_running_model() => _refused.ShouldBeTrue();
    [Fact] void should_keep_the_blanket_api_refusal() => _patterns.ShouldContain("/api/{**path}");

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    public async Task should_still_refuse_application_requests(string method)
    {
        var context = new DefaultHttpContext { RequestServices = _app.Services };
        context.Request.Method = method;
        context.Request.Path = "/api/projects/registration";
        context.Request.RouteValues["path"] = "projects/registration";
        context.Response.Body = new MemoryStream();
        var endpoint = ((IEndpointRouteBuilder)_app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().Single(endpoint => endpoint.RoutePattern.RawText == "/api/{**path}");
        await endpoint.RequestDelegate!(context);
        context.Response.StatusCode.ShouldEqual(StatusCodes.Status501NotImplemented);
    }
}
