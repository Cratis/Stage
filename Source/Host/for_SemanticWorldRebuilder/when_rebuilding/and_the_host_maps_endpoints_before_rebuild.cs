// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class and_the_host_maps_endpoints_before_rebuild : a_rebuildable_world
{
    WebApplication _app = null!;
    SemanticWorld? _world;
    string _loadingState = string.Empty;
    string _readyState = string.Empty;
    int _requestStatus;
    int _mappedEndpoints;
    int _readModels;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        var routes = StageHttpRouteOptions.FromConfiguration(builder.Configuration);
        var surface = StageHttpSurface.Create(_plan.Model, routes);
        builder.AddStageCratis("semantic-mapping-spec", "Semantic mapping spec", routes);
        builder.Services.AddSingleton(new StageEventStoreName("semantic-mapping-spec"));
        SemanticRuntimeHosting.Add(builder.Services, _plan, SemanticHost.WorldProvider(() => _world));
        builder.Services.AddSingleton(Substitute.For<IAppendSemanticFacts>());
        builder.Services.AddSingleton<DynamicTypeFactory>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddControllers();
        _app = builder.Build();
        _app.UseRouting();
        SemanticHost.UseReadinessGate(_app, () => _world, []);
        StageEndpointMapper.Map(_app, surface);
        _app.UseCratisArc();
        _mappedEndpoints = ((IEndpointRouteBuilder)_app).DataSources.Sum(source => source.Endpoints.Count);
        _loadingState = SemanticHost.RegistrationStatus(_world, [], _app.Services, "Projects", "project-model").State;

        var pipeline = new ApplicationBuilder(_app.Services);
        pipeline.UseRouting();
        SemanticHost.UseReadinessGate(pipeline, () => _world, []);
        pipeline.UseEndpoints(endpoints =>
        {
            foreach (var source in ((IEndpointRouteBuilder)_app).DataSources)
            {
                endpoints.DataSources.Add(source);
            }
        });
        var context = new DefaultHttpContext { RequestServices = _app.Services };
        context.Request.Method = "GET";
        context.Request.Path = surface.Operations.First(operation => operation.Method == "GET").CanonicalPath;
        context.Response.Body = new MemoryStream();
        await pipeline.Build()(context);
        _requestStatus = context.Response.StatusCode;

        var result = new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, SemanticExecutionRequest.Create(_command.Id, _commandValues, []) with
        {
            Occurrence = new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero), "owner", "Owner", "owner")
        });
        _world = (result as SemanticAccepted)!.World;
        _readModels = (await _app.Services.GetRequiredService<ISemanticRuntime>().ReadModels(_readModel.Id)).Length;
        _readyState = SemanticHost.RegistrationStatus(_world, [], _app.Services, "Projects", "project-model").State;
    }

    async Task Destroy()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    [Fact] void should_map_the_real_host_endpoints_without_a_world() => _mappedEndpoints.ShouldBeGreaterThan(0);
    [Fact] void should_report_loading_before_rebuild() => _loadingState.ShouldEqual("loading");
    [Fact] void should_refuse_requests_before_rebuild() => _requestStatus.ShouldEqual(StatusCodes.Status503ServiceUnavailable);
    [Fact] void should_use_the_reconstructed_world_after_rebuild() => _readModels.ShouldEqual(1);
    [Fact] void should_report_ready_after_rebuild() => _readyState.ShouldEqual("ready");
}
