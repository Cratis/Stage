// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;
using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Microsoft.Extensions.Options;
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
    string[] _dependencies = [];
    string[] _handlers = [];
    string _directory = null!;
    protected virtual bool Semantic => true;

    async Task Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage-startup-spec-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "Projects.play");
        await File.WriteAllTextAsync(path, specification_plan.Source());
        var plan = (await SemanticModelLoader.LoadFromPathAsync(path)).Plan;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = ["--urls", "http://127.0.0.1:0"] });
        builder.AddStageCratis("SpecificationHost", "Stage specification startup");
        builder.Services.AddSingleton(new StageEventStoreName("SpecificationHost"));
        if (Semantic)
        {
            SemanticRuntimeHosting.Add(builder.Services, plan, SemanticWorld.Empty);
        }
        else
        {
            builder.Services.AddSingleton(await EventModelLoader.LoadFromPathAsync(path));
            builder.Services.AddSingleton<DynamicTypeFactory>();
            builder.Services.AddSingleton(Substitute.For<IAppendProducedEvents>());
            builder.Services.AddSingleton(Substitute.For<IProvideStageIdentity>());
        }
        builder.Services.AddSingleton<ISpecificationRunProcess, SpecificationRunProcess>();
        builder.Services.AddSingleton(SemanticSpecificationProcessOptions.FromConfiguration(builder.Configuration));

        // Keep production discovery; replace only the transport to the external kernel.
        var client = Substitute.For<IChronicleClient>();
        var eventStore = Substitute.For<IEventStore>();
        client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(eventStore);
        builder.Services.AddSingleton(client);
        _app = builder.Build();
        _app.UseCratisChronicle();
        _app.MapGet("/stage/status", () => Semantic
            ? SemanticHost.RegistrationStatus(SemanticWorld.Empty, [], _app.Services, "Projects", path)
            : new StageStatus("ready", new StageStatusModel("Projects"), null) { Engine = "eventmodel" });
        SemanticSpecificationRuns.Map(_app, plan, path);
        _app.UseCratisArc();
        await _app.StartAsync();
        _http = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()), Timeout = TimeSpan.FromSeconds(120) };
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
        using var deps = JsonDocument.Parse(await File.ReadAllTextAsync(Path.ChangeExtension(typeof(SemanticHost).Assembly.Location, ".deps.json")));
        _dependencies = [.. deps.RootElement.GetProperty("libraries").EnumerateObject().Select(entry => entry.Name.Split('/')[0])];
        _handlers = [.. _app.Services.GetRequiredService<ICommandHandlerProviders>().Handlers.Select(handler => handler.CommandType.Assembly.GetName().Name!)];
    }

    [Fact] void should_leave_chronicle_artifact_registration_to_stage() => _app.Services.GetRequiredService<IOptions<ChronicleOptions>>().Value.AutoDiscoverAndRegister.ShouldBeFalse();
    [Fact] void should_start_and_serve_status() => _status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_keep_the_host_ready_after_a_run() => _stageStatus.State.ShouldEqual("ready");
    [Fact] void should_run_the_specification_in_the_child() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_have_no_kernel_or_testing_dependencies() => _dependencies.Where(name => new[] { "Cratis.Chronicle.Core", "Cratis.Chronicle.Grpc" }.Contains(name, StringComparer.Ordinal) || name.Contains(".Testing", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_discover_only_stage_command_handlers() => _handlers.Where(name => !new[] { "Cratis.Stage", "Cratis.Stage.Host", "Stage.Generated" }.Contains(name, StringComparer.Ordinal)).ShouldBeEmpty();

    async Task Destroy()
    {
        _http?.Dispose();
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
        Directory.Delete(_directory, recursive: true);
    }
}
