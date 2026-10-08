// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using NSubstitute;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns.given;

public class a_specification_endpoint : Specification
{
    protected WebApplication _app = null!;
    protected SemanticExecutionPlan _plan = null!;
    protected IEventStore _hostEventStore = null!;
    protected int _status;
    protected string _body = null!;
    protected SemanticSpecificationRunReport Report => SemanticSpecificationRunReportFile.Read(_body)!;
    protected virtual bool Seeded => false;
    protected virtual bool Refused => false;
    protected virtual bool LiveRegistrationRefused => false;
    private protected virtual ISpecificationRunProcess Process => new SpecificationRunProcess();
    private protected virtual TimeSpan Timeout => TimeSpan.FromSeconds(120);
    protected string _modelPath = null!;
    protected string _directory = null!;
    RequestDelegate _request = null!;

    async Task Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-host-spec-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _modelPath = Path.Combine(_directory, "Projects.play");
        await File.WriteAllTextAsync(_modelPath, specification_plan.Source(Seeded));
        _plan = (await SemanticModelLoader.LoadFromPathAsync(_modelPath)).Plan;
        _hostEventStore = Substitute.For<IEventStore>();
        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        builder.Services.AddSingleton(_hostEventStore);
        builder.Services.AddSingleton(Process);
        builder.Services.AddSingleton(new SemanticSpecificationProcessOptions(Path.Combine(AppContext.BaseDirectory, "specrunner", "Cratis.Stage.SpecRunner.dll"), Timeout));
        _app = builder.Build();
        if (Refused)
        {
            SemanticHost.MapRefused(_app, [new StageUnsupportedIssue("Plan", "model", "The model is not admitted.")]);
        }
        else
        {
            SemanticSpecificationRuns.Map(_app, _plan, _modelPath);
        }
        var pipeline = new ApplicationBuilder(_app.Services);
        if (LiveRegistrationRefused) SemanticHost.UseReadinessGate(pipeline, () => null, [new StageUnsupportedIssue("World", "model", "Chronicle registration was refused.")]);
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

    protected async Task Request(string body, CancellationToken cancellationToken = default)
    {
        await using var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(body));
        await using var responseBody = new MemoryStream();
        await using var scope = _app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider, RequestAborted = cancellationToken };
        context.Features.Set<IHttpRequestBodyDetectionFeature>(new body_detection());
        context.Request.Method = "POST";
        context.Request.Path = SemanticSpecificationRuns.Route;
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = requestBody.Length;
        context.Request.Body = requestBody;
        context.Response.Body = responseBody;
        await _request(context);
        _status = context.Response.StatusCode;
        _body = Encoding.UTF8.GetString(responseBody.ToArray());
    }

    async Task Destroy()
    {
        await _app.DisposeAsync();
        Directory.Delete(_directory, recursive: true);
    }

    sealed class body_detection : IHttpRequestBodyDetectionFeature
    {
        public bool CanHaveBody => true;
    }
}
