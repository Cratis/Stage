// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Screenplay;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_a_routed_model : given.a_model_path
{
    [Fact]
    public async Task should_refuse_the_legacy_model_and_serve_no_application_api()
    {
        await File.WriteAllTextAsync(_path, "eventsource Invoice\n  stream Changes\nmodule Billing\n  feature Invoices\n    slice StateChange Register\n      event InvoiceRegistered\n");
        var issues = new List<StageUnsupportedIssue>();
        var application = await EventModelHost.LoadModel(_path, issues);
        application.ShouldBeNull();
        var issue = Assert.Single(issues);
        issue.Capability.ShouldEqual("Plan");
        issue.Artifact.ShouldEqual("model");
        issue.Details.StartsWith("STAGE-ESM-030:", StringComparison.Ordinal).ShouldBeTrue();

        await using var app = WebApplication.CreateBuilder().Build();
        SemanticHost.MapRefused(app, issues, "eventmodel");
        var pipeline = new ApplicationBuilder(app.Services);
        pipeline.UseRouting();
        pipeline.UseEndpoints(endpoints =>
        {
            foreach (var source in ((IEndpointRouteBuilder)app).DataSources) endpoints.DataSources.Add(source);
        });
        var request = pipeline.Build();
        await using var scope = app.Services.CreateAsyncScope();
        foreach (var path in new[] { "/stage/status", "/api/commands/RegisterInvoice", "/api/queries/Invoices" })
        {
            await using var body = new MemoryStream();
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Method = "GET";
            context.Request.Path = path;
            context.Response.Body = body;
            await request(context);
            var response = System.Text.Encoding.UTF8.GetString(body.ToArray());
            if (path.StartsWith("/api", StringComparison.Ordinal))
            {
                context.Response.StatusCode.ShouldEqual(StatusCodes.Status501NotImplemented);
                context.Response.Headers["Stage-Unsupported-Capability"].ToString().ShouldEqual("Plan");
            }
            else
            {
                context.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
                response.ShouldContain("unsupported");
                response.ShouldContain("eventmodel");
            }
            response.ShouldContain(UnsupportedEventSourceRoutes.DiagnosticCode);
        }
    }
}
