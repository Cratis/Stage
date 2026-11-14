// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
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
using Xunit.Sdk;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.given;

public class a_browser : a_routed_model
{
    readonly ConcurrentQueue<string> _logged = new();
    protected HttpClient _client = null!;
    protected IAppendSemanticFacts _facts = null!;
    protected bool _ready = true;
    protected bool _failAppend;
    protected virtual bool Refused => false;
    protected virtual string Engine => "semantic";
    protected virtual string ModelSource => Source;
    protected SemanticId ModelId => _semanticRuntime.Plan.ReadModels.Values.Single(model => model.Name == "ProjectSummary").Id;
    protected const string CommandRoute = "/api/projects/registration/register-project/register-project";
    protected const string Payload = """
        {"projectId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","name":"Screenplay","tags":["one","two"],"active":true,"count":7,"address":{"city":"Oslo"}}
        """;

    protected override void ConfigureHostServices(IServiceCollection services) =>
        services.AddLogging(logging => logging.AddProvider(new CapturingLoggerProvider(_logged)));

    async Task Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("browser-model"), "browser-model", "Projects.play", ModelSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model, []);
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
        MapSemanticModel(EventModelLoader.LoadFromSource(ModelSource), plan.Plan, _facts);
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
            SemanticReadModelBrowser.Map(_app, _semanticRuntime, () => _ready);
        }
        _app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (Exception exception)
            {
                _logged.Enqueue(exception.ToString());
                throw;
            }
        });
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        _client = new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    protected async Task Register(string payload = Payload)
    {
        using var response = await _client.PostAsync(CommandRoute, new StringContent(payload, Encoding.UTF8, "application/json"));
        await EnsureSuccess(response);
    }

    protected async Task<JsonElement> Json(string path)
    {
        using var response = await _client.GetAsync(path);
        await EnsureSuccess(response);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.Clone();
    }

    async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        var server = string.Join(Environment.NewLine, _logged);
        throw new XunitException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase} from {response.RequestMessage?.RequestUri}\nBody:\n{(body.Length == 0 ? "<empty>" : body)}\nServer:\n{(server.Length == 0 ? "<none captured>" : server)}");
    }

    void Destroy() => _client.Dispose();

    sealed class CapturingLoggerProvider(ConcurrentQueue<string> logged) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(logged, categoryName);

        public void Dispose() { }
    }

    sealed class CapturingLogger(ConcurrentQueue<string> logged, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel is >= LogLevel.Warning and < LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel)) logged.Enqueue($"{logLevel} {category}: {formatter(state, exception)}{Environment.NewLine}{exception}");
        }
    }

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
