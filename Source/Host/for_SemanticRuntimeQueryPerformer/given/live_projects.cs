// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Channels;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.Http;

namespace Cratis.Stage.Host.for_SemanticRuntimeQueryPerformer.given;

public class live_projects : Specification
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);

    protected ISemanticRuntime _runtime = null!;
    protected IQueryPerformer _performer = null!;
    SemanticRuntimeQueryPerformerProvider _provider = null!;
    SemanticCommand _register = null!;
    SemanticCommand _tag = null!;

    void Establish()
    {
        const string source = """
            module Projects
              feature Registration
                slice StateChange Register
                  command RegisterProject
                    projectId String identifier
                    name String
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      name = name
                  event ProjectRegistered
                    projectId String
                    name String
                slice StateChange Tag
                  command TagProject
                    tagId String identifier
                    label String
                    produces ProjectTagged
                      for tagId
                      tagId = tagId
                      label = label
                  event ProjectTagged
                    tagId String
                    label String
                slice StateView List
                  readmodel ProjectSummary
                    projectId String
                    name String
                  query LiveProjects => observable ProjectSummary[]
                  query LiveProject => observable ProjectSummary?
                    by projectId String
                  query AllProjects => ProjectSummary[]
                  projection ProjectSummaryProjection => ProjectSummary
                    from ProjectRegistered key projectId
                      projectId = projectId
                      name = name
                slice StateView Tags
                  readmodel TagSummary
                    tagId String
                    label String
                  query AllTags => TagSummary[]
                  projection TagSummaryProjection => TagSummary
                    from ProjectTagged key tagId
                      tagId = tagId
                      label = label
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("projects"), "projects", "Projects.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _register = plan.Commands.Values.Single(command => command.Name == "RegisterProject");
        _tag = plan.Commands.Values.Single(command => command.Name == "TagProject");
        _runtime = new a_semantic_fact_log().Runtime(plan, SemanticWorld.Empty);
        _provider = new([_runtime], [new DynamicTypeFactory()], [new HttpContextAccessor { HttpContext = new DefaultHttpContext() }]);
    }

    protected async Task<object?> Perform(string name, QueryArguments? arguments = null)
    {
        _performer = _provider.Performers.Single(performer => performer.Name == name);
        return await _performer.Perform(new QueryContext(_performer.FullyQualifiedName, CorrelationId.NotSet, Paging.NotPaged, Sorting.None, arguments ?? QueryArguments.Empty));
    }

    protected Task Register(string id, string name) => Execute(_register, ("projectId", id), ("name", name));

    protected Task Tag(string id, string label) => Execute(_tag, ("tagId", id), ("label", label));

    protected static async Task<T> Next<T>(Channel<T> emissions) => await emissions.Reader.ReadAsync().AsTask().WaitAsync(_deadline);

    protected static IEnumerable<string> Names(IEnumerable<object> projects) => projects.Cast<DynamicReadModel>().Select(project => project.Id);

    protected static Channel<T> Observe<T>(IObservable<T> subject, out IDisposable subscription)
    {
        var emissions = Channel.CreateUnbounded<T>();
        subscription = subject.Subscribe(new Observer<T>(emissions));
        return emissions;
    }

    async Task Execute(SemanticCommand command, params (string Name, string Value)[] values)
    {
        var result = await _runtime.Execute(
            command,
            values.ToDictionary(value => value.Name, value => JsonSerializer.SerializeToElement(value.Value)),
            new ClaimsPrincipal(),
            new(DateTimeOffset.UtcNow, "subject", "name", "user"),
            false);
        result.ShouldBeOfExactType<SemanticAccepted>();
    }

    sealed class Observer<T>(Channel<T> emissions) : IObserver<T>
    {
        public void OnNext(T value) => emissions.Writer.TryWrite(value);

        public void OnError(Exception error) => emissions.Writer.TryComplete(error);

        public void OnCompleted() => emissions.Writer.TryComplete();
    }
}
