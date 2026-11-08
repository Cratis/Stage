// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using Cratis.Specifications;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Stage.Host.for_StageJourneyReplay.given;

public class a_replay_endpoint : Specification
{
    protected const string OneJourney = """{ "schema": "stage-journey/v1", "journeys": [{ "name": "register", "steps": [{ "command": "RegisterProject" }] }] }""";

    protected WebApplication _app = null!;
    protected readonly List<Uri> _sent = [];
    protected string _state = "ready";
    RequestDelegate _request = null!;

    protected virtual Task<HttpResponseMessage> Respond(HttpRequestMessage request) =>
        Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"response":{},"isSuccess":true}""", Encoding.UTF8, "application/json") });

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IServer>(new fake_server());
        builder.Services.AddSingleton(new StageJourneyOptions(TimeSpan.FromSeconds(30)));
        builder.Services.AddSingleton(_ => new StageJourneyClient(new fake_handler(this)));
        _app = builder.Build();
        var routes = new StageRoutes(new Dictionary<string, string> { ["RegisterProject"] = "/api/projects/register" }, new Dictionary<string, string>());
        StageJourneyReplay.Map(_app, () => routes, () => new StageStatus(_state, null, null) { Issues = _state == "unsupported" ? [new StageUnsupportedIssue("World", "model", "Chronicle registration was refused.")] : null });
        var pipeline = new ApplicationBuilder(_app.Services);
        pipeline.UseRouting();
        pipeline.UseEndpoints(endpoints =>
        {
            foreach (var source in ((IEndpointRouteBuilder)_app).DataSources)
            {
                endpoints.DataSources.Add(source);
            }
        });
        _request = pipeline.Build();
    }

    protected async Task<(int Status, string Body)> Request(string body, string query = "")
    {
        await using var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(body));
        await using var responseBody = new MemoryStream();
        await using var scope = _app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Features.Set<IHttpRequestBodyDetectionFeature>(new body_detection());
        context.Request.Method = "POST";
        context.Request.Path = StageJourneyReplay.Route;
        context.Request.QueryString = new QueryString(query);
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = requestBody.Length;
        context.Request.Body = requestBody;
        context.Response.Body = responseBody;
        await _request(context);

        return (context.Response.StatusCode, Encoding.UTF8.GetString(responseBody.ToArray()));
    }

    async Task Destroy() => await _app.DisposeAsync();

    sealed class body_detection : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }

    sealed class fake_server : IServer
    {
        public IFeatureCollection Features { get; } = Addresses();

        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
            where TContext : notnull => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose()
        {
        }

        static FeatureCollection Addresses()
        {
            var features = new FeatureCollection();
            var addresses = new ServerAddressesFeature();
            addresses.Addresses.Add("https://[::]:9443");
            addresses.Addresses.Add("http://+:9090");
            features.Set<IServerAddressesFeature>(addresses);

            return features;
        }
    }

    sealed class fake_handler(a_replay_endpoint owner) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            owner._sent.Add(request.RequestUri!);

            return owner.Respond(request);
        }
    }
}
