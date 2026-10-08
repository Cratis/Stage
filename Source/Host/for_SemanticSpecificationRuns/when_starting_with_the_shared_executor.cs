// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Cratis.Stage.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_starting_with_the_shared_executor : Specification
{
    WebApplication _app = null!;
    HttpClient _http = null!;
    HttpStatusCode _status;
    StageStatus _stageStatus = null!;
    SemanticSpecificationRunReport _report = null!;
    Type[] _artifacts = [];
    Type[] _handlers = [];

    async Task Establish()
    {
        var plan = specification_plan.Create();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = ["--urls", "http://127.0.0.1:0"] });
        builder.AddStageCratis("SpecificationHost", "Stage specification startup");
        builder.Services.AddSingleton(new StageEventStoreName("SpecificationHost"));
        SemanticRuntimeHosting.Add(builder.Services, plan, SemanticWorld.Empty);
        builder.Services.AddSingleton<ISemanticSpecificationExecutor>(new SemanticSpecificationExecutor());

        // Keep the production composition and discovery; only replace the transport to the external kernel.
        var client = Substitute.For<IChronicleClient>();
        var eventStore = Substitute.For<IEventStore>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(eventStore);
        builder.Services.AddSingleton(client);
        _app = builder.Build();
        _app.UseCratisChronicle();
        _app.MapGet("/stage/status", () => SemanticHost.RegistrationStatus(SemanticWorld.Empty, [], _app.Services, "Projects", "Model.play"));
        SemanticSpecificationRuns.Map(_app, plan);
        _app.UseCratisArc();
        await _app.StartAsync();
        _http = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()), Timeout = TimeSpan.FromSeconds(30) };
    }

    async Task Because()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var run = await _http.PostAsync(SemanticSpecificationRuns.Route, content);
        var reportBody = await run.Content.ReadAsStringAsync();
        Assert.True(run.IsSuccessStatusCode, $"Run returned {(int)run.StatusCode}: {reportBody}");
        _report = SemanticSpecificationRunReportFile.Read(reportBody)!;
        using var status = await _http.GetAsync("/stage/status");
        _status = status.StatusCode;
        _stageStatus = JsonSerializer.Deserialize<StageStatus>(await status.Content.ReadAsStringAsync(), StageJson.Options)!;

        var artifacts = DefaultClientArtifactsProvider.Default;
        _artifacts = [.. artifacts.EventTypes, .. artifacts.Projections, .. artifacts.ModelBoundProjections, .. artifacts.Reactors, .. artifacts.Reducers, .. artifacts.ConstraintTypes];
        _handlers = [.. _app.Services.GetRequiredService<ICommandHandlerProviders>().Handlers.Select(handler => handler.CommandType),
            .. _app.Services.GetRequiredService<IQueryPerformerProviders>().Performers.Select(performer => performer.ReadModelType)];
    }

    [Fact] void should_start_and_serve_status() => _status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_keep_the_semantic_host_ready_after_a_run() => _stageStatus.State.ShouldEqual("ready");
    [Fact] void should_run_the_specification() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_not_discover_testing_or_per_run_chronicle_artifacts() => _artifacts.Where(IsRunArtifact).ShouldBeEmpty();
    [Fact] void should_not_discover_testing_or_per_run_arc_artifacts() => _handlers.Where(IsRunArtifact).ShouldBeEmpty();

    static bool IsRunArtifact(Type type) => (type.Assembly.IsDynamic && type.Namespace?.StartsWith("Stage.Semantic.", StringComparison.Ordinal) == true) || (type.Assembly.GetName().Name is { } name &&
        (name.Contains("Testing", StringComparison.Ordinal) || name == "Cratis.Stage.Specifications"));

    async Task Destroy()
    {
        _http?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
