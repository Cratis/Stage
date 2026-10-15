// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Routing;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_reporting_successful_load_notes : given.a_model_path
{
    JsonDocument _status = null!;

    Task Establish() => File.WriteAllTextAsync(_path, """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                description "Register a project"
                projectId Uuid identifier
                produces ProjectRegistered
                  for projectId
              event ProjectRegistered
        """);

    async Task Because()
    {
        var notes = new List<SemanticModelLoadDiagnostic>();
        var issues = new List<StageUnsupportedIssue>();
        await SemanticHost.LoadModel(_path, issues, notes);
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(Substitute.For<ISemanticRuntime>());
        await using var app = builder.Build();
        SemanticHost.MapStatus(app, () => SemanticHost.Status(app.Services.GetRequiredService<ISemanticRuntime>(), "Projects", _path), notes);
        var pipeline = new ApplicationBuilder(app.Services);
        pipeline.UseRouting();
        pipeline.UseEndpoints(endpoints =>
        {
            foreach (var source in ((IEndpointRouteBuilder)app).DataSources) endpoints.DataSources.Add(source);
        });
        await using var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = "GET";
        context.Request.Path = "/stage/status";
        context.Response.Body = body;
        await pipeline.Build()(context);
        _status = JsonDocument.Parse(body.ToArray());
    }

    void Destroy() => _status.Dispose();

    [Fact] void should_report_ready() => _status.RootElement.GetProperty("state").GetString().ShouldEqual("ready");
    [Fact] void should_expose_notes_on_successful_status() => _status.RootElement.GetProperty("notes").GetArrayLength().ShouldEqual(1);
    [Fact] void should_preserve_the_information_code() => _status.RootElement.GetProperty("notes")[0].GetProperty("code").GetString().ShouldEqual("PLAY0270");
    [Fact] void should_preserve_information_severity() => _status.RootElement.GetProperty("notes")[0].GetProperty("severity").GetInt32().ShouldEqual((int)DiagnosticSeverity.Information);
    [Fact] void should_not_report_blocking_issues() => _status.RootElement.TryGetProperty("issues", out _).ShouldBeFalse();
}
