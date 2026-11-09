// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_the_instance_was_never_created : Specification
{
    const string Source = """
        module Tracking
          feature Tasks
            slice StateView Overview
              event TaskCreated
                projectId String
              event TaskCompleted
              command CreateTask
                id String identifier
                projectId String
                produces TaskCreated
                  for id
                  projectId = projectId
              command CompleteTask
                id String identifier
                produces TaskCompleted
                  for id
              readmodel TaskOverview
                id String key
                projectId String
              projection TaskOverviewProjection => TaskOverview
                from TaskCreated key $eventSourceId
                  id = $eventSourceId
                  projectId = projectId
                from TaskCompleted key $eventSourceId
        """;

    a_semantic_fact_log _log = null!;
    SemanticExecutionResult _outcome = null!;
    CommandResult _result = null!;
    CommandResult _created = null!;
    DefaultHttpContext _http = null!;

    async Task Establish()
    {
        const string application = "NeverCreated";
        var syntax = new ScreenplayCompiler().Parse(Source);
        Assert.True(syntax.Success, string.Join(';', syntax.Diagnostics));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(application));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", Source);
        var bound = new SemanticModelBinder().Bind(application, syntax.Value!, SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join(';', bound.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(bound.Value!.Model).Plan!;
        _log = new a_semantic_fact_log();
        _http = new();
        var runtime = _log.Runtime(plan, SemanticWorld.Empty);
        var complete = plan.Commands.Values.Single(command => command.Name == "CompleteTask");
        var create = plan.Commands.Values.Single(command => command.Name == "CreateTask");

        _outcome = await Execute(runtime, complete, new { id = "missing" });
        _result = SemanticCommandOutcome.Map(_outcome, CorrelationId.NotSet, _http, complete.Id.ToString());
        var created = await Execute(runtime, create, new { id = "known", projectId = "p" });
        _created = SemanticCommandOutcome.Map(created, CorrelationId.NotSet, new DefaultHttpContext(), create.Id.ToString());
    }

    [Fact] void should_not_append_anything() => _log.Facts.Count.ShouldEqual(1);
    [Fact] void should_refuse_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_be_invalid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_report_an_exception() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_say_the_instance_does_not_exist() => _result.ValidationResults.Single().Message.ShouldEqual("The instance does not exist.");
    [Fact] void should_not_leak_the_internal_exception_text() => _result.ValidationResults.Single().Message.ShouldNotContain("did not establish");
    [Fact] void should_not_mark_the_response_unsupported() => _http.Items.ContainsKey(SemanticRuntimeMarkers.Unsupported).ShouldBeFalse();
    [Fact] void should_not_leak_the_model_identity() => _http.Response.Headers.ContainsKey("Stage-Unsupported-Artifact").ShouldBeFalse();
    [Fact] void should_still_accept_a_creating_command() => _created.IsSuccess.ShouldBeTrue();

    static Task<SemanticExecutionResult> Execute(ISemanticRuntime runtime, SemanticCommand command, object payload)
    {
        var json = JsonSerializer.SerializeToElement(payload);
        var values = command.Properties.Where(property => json.TryGetProperty(property.Name, out _)).ToDictionary(property => property.Name, property => json.GetProperty(property.Name));
        return runtime.Execute(command, values, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"), validateOnly: false);
    }
}
