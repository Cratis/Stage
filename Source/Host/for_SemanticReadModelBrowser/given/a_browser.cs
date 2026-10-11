// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host.for_StageEndpointMapper.given;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.given;

public class a_browser : a_routed_model
{
    protected HttpClient _client = null!;
    protected IAppendSemanticFacts _facts = null!;
    protected bool _ready = true;
    protected bool _failAppend;
    protected virtual bool Refused => false;
    protected virtual string Engine => "semantic";
    protected SemanticId ModelId => _semanticRuntime!.Plan.ReadModels.Values.Single(model => model.Name == "ProjectSummary").Id;
    protected const string CommandRoute = "/api/projects/registration/register-project/register-project";
    protected const string Payload = """
        {"projectId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","name":"Screenplay","tags":["one","two"],"active":true,"count":7,"address":{"city":"Oslo"}}
        """;

    async Task Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("browser-model"), "browser-model", "Projects.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.True(plan.Success, string.Join("; ", plan.Issues.Select(issue => issue.Details)));
        _facts = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        var tail = ulong.MaxValue;
        ((ISemanticFactTail)_facts).Tail().Returns(_ => tail);
        _facts.Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>()).Returns(call =>
        {
            if (_failAppend)
            {
                return Task.FromException(new SemanticCommandExecutionFailed("Acknowledgment lost."));
            }
            tail = tail == ulong.MaxValue ? (ulong)call.Arg<IReadOnlyList<SemanticFact>>().Count - 1 : tail + (ulong)call.Arg<IReadOnlyList<SemanticFact>>().Count;
            return Task.CompletedTask;
        });
        MapSemanticModel(EventModelLoader.LoadFromSource(Source), plan.Plan!, _facts);
        if (Engine == "eventmodel")
        {
            SemanticReadModelBrowser.MapEventModel(_app);
            _app.MapFallback(() => Results.Text("SPA fallback"));
        }
        if (Refused)
        {
            SemanticHost.MapRefused(_app, [new StageUnsupportedIssue("Plan", "model", "The model is not admitted.")], Engine);
        }
        else if (Engine == "semantic")
        {
            SemanticReadModelBrowser.Map(_app, _semanticRuntime!, () => _ready);
        }
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        _client = new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    protected async Task Register(string payload = Payload)
    {
        using var response = await _client.PostAsync(CommandRoute, new StringContent(payload, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
    }

    protected async Task<JsonElement> Json(string path)
    {
        using var response = await _client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }

    void Destroy() => _client.Dispose();

    const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
        type Address
          city String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                tags String[]
                active Bool
                count Int
                address Address
                produces ProjectRegistered
                  for projectId
                  name = name
                  tags = tags
                  active = active
                  count = count
                  address = address
              event ProjectRegistered
                name ProjectName
                tags String[]
                active Bool
                count Int
                address Address
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
                tags String[]
                active Bool
                count Int
                address Address
              query ProjectById => ProjectSummary optional
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key $eventSourceId
                  projectId = $eventSourceId
                  name = name
                  tags = tags
                  active = active
                  count = count
                  address = address
            feature History
              slice StateView EmptyLookup
                readmodel EmptySummary
                  name ProjectName optional
                  emptySummaryId ProjectId key
        """;
}
