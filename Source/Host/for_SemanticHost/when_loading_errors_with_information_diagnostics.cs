// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_errors_with_information_diagnostics : given.a_model_path
{
    readonly List<StageUnsupportedIssue> _issues = [];
    readonly List<SemanticModelLoadDiagnostic> _notes = [];
    LoadedSemanticModel? _loaded;
    SceneApplication? _scene;
    JsonDocument _status = null!;
    string _refusal = null!;

    Task Establish() => File.WriteAllTextAsync(_path, """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                description "Register a project"
                projectId Uuid identifier
                produces UnknownEvent
                  for projectId
              event ProjectRegistered
                description "A project was registered"
        """);

    async Task Because()
    {
        (_loaded, _scene) = await SemanticHost.LoadModel(_path, _issues, _notes);
        await using var app = WebApplication.CreateBuilder().Build();
        SemanticHost.MapRefused(app, _issues, notes: _notes);
        var pipeline = new ApplicationBuilder(app.Services);
        pipeline.UseRouting();
        pipeline.UseEndpoints(endpoints =>
        {
            foreach (var source in ((IEndpointRouteBuilder)app).DataSources) endpoints.DataSources.Add(source);
        });
        var request = pipeline.Build();
        await using var scope = app.Services.CreateAsyncScope();
        _status = JsonDocument.Parse(await Request(request, scope.ServiceProvider, "/stage/status"));
        _refusal = await Request(request, scope.ServiceProvider, "/api/commands/RegisterProject");
    }

    void Destroy() => _status.Dispose();

    static async Task<string> Request(RequestDelegate request, IServiceProvider services, string path)
    {
        await using var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "GET";
        context.Request.Path = path;
        context.Response.Body = body;
        await request(context);

        return Encoding.UTF8.GetString(body.ToArray());
    }

    [Fact] void should_not_load_the_semantic_model() => _loaded.ShouldBeNull();
    [Fact] void should_not_serve_a_scene() => _scene.ShouldBeNull();
    [Fact] void should_report_only_one_blocking_error() => _issues.Count.ShouldEqual(1);
    [Fact] void should_report_a_plan_issue() => _issues.Single().Capability.ShouldEqual("Plan");
    [Fact] void should_keep_information_out_of_plan_issues() => _issues.Exists(issue => _notes.Exists(note => issue.Details.Contains(note.Message, StringComparison.Ordinal))).ShouldBeFalse();
    [Fact] void should_keep_all_nonblocking_notes() => _notes.Count.ShouldEqual(3);
    [Fact] void should_preserve_information_severity() => _notes.Count(note => note.Severity == DiagnosticSeverity.Information).ShouldEqual(2);
    [Fact] void should_preserve_warning_severity() => _notes.Count(note => note.Severity == DiagnosticSeverity.Warning).ShouldEqual(1);
    [Fact] void should_expose_notes_to_clients_on_status() => _status.RootElement.GetProperty("notes").GetArrayLength().ShouldEqual(3);
    [Fact] void should_preserve_note_codes_in_json() => _status.RootElement.GetProperty("notes").EnumerateArray().Count(note => note.GetProperty("code").GetString() == "PLAY0270").ShouldEqual(2);
    [Fact] void should_preserve_note_locations_in_json() => _status.RootElement.GetProperty("notes")[0].GetProperty("source").GetString()!.StartsWith("model.play(", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_only_one_status_issue() => _status.RootElement.GetProperty("issues").GetArrayLength().ShouldEqual(1);
    [Fact] void should_not_format_notes_as_unsupported_plan_responses() => _notes.Exists(note => _refusal.Contains(note.Message, StringComparison.Ordinal)).ShouldBeFalse();
}
