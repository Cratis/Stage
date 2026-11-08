// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_asking_for_the_status_with_a_non_numeric_wait : Specification
{
    int _statusCode;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        await using var app = builder.Build();
        SemanticHost.MapStatus(app, () => new StageStatus(StageStatusWait.Loading, null, null));
        var pipeline = new ApplicationBuilder(app.Services);
        pipeline.UseRouting();
        pipeline.UseEndpoints(endpoints =>
        {
            foreach (var source in ((IEndpointRouteBuilder)app).DataSources) endpoints.DataSources.Add(source);
        });
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = "GET";
        context.Request.Path = "/stage/status";
        context.Request.QueryString = new QueryString("?wait=soon");
        context.Response.Body = new MemoryStream();
        await pipeline.Build()(context);
        _statusCode = context.Response.StatusCode;
    }

    [Fact] void should_reject_the_request() => _statusCode.ShouldEqual(StatusCodes.Status400BadRequest);
}
