// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Cratis.Stage.Semantics.for_SemanticRuntimeQueryPerformerProvider.given;

public class a_list_query_runtime : Specification
{
    protected ISemanticRuntime _runtime = null!;
    protected DynamicTypeFactory _factory = null!;
    protected IHttpContextAccessor _http = null!;
    protected SemanticRuntimeQueryPerformerProvider _provider = null!;
    protected IQueryPerformer _performer = null!;
    protected object? _result;

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
                slice StateView List
                  readmodel ProjectSummary
                    projectId String
                    name String
                  query AllProjects => ProjectSummary[]
                  query ProjectsByName => ProjectSummary[]
                    by name String
                  projection ProjectSummaryProjection => ProjectSummary
                    from ProjectRegistered key projectId
                      projectId = projectId
                      name = name
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("projects"), "projects", "Projects.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var world = SemanticWorld.Empty;
        var command = plan.Commands.Values.Single();
        foreach (var (id, name) in new[] { ("project-1", "Shared"), ("project-2", "Shared"), ("project-3", "Other") })
        {
            ImmutableArray<SemanticPropertyValue> values =
            [
                new(command.Properties.Single(property => property.Name == "projectId").Id, SemanticValue.Text(id)),
                new(command.Properties.Single(property => property.Name == "name").Id, SemanticValue.Text(name))
            ];
            var result = new SemanticEvaluator().Execute(plan, world, SemanticExecutionRequest.Create(command.Id, values, []));
            result.ShouldBeOfExactType<SemanticAccepted>();
            world = result.World;
        }

        _runtime = Substitute.For<ISemanticRuntime>();
        _runtime.Plan.Returns(plan);
        _runtime.Query(Arg.Any<SemanticKeyedQuery>(), Arg.Any<SemanticValue>(), Arg.Any<ClaimsPrincipal>()).Returns(call =>
            new SemanticEvaluator().Execute(plan, world, SemanticExecutionRequest.ForQueries([new(call.Arg<SemanticKeyedQuery>().Id, call.Arg<SemanticValue>())])));
        _factory = new();
        _http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
    }

    protected void CreateProvider() => _provider = new([_runtime], [_factory], [_http]);

    protected async Task Perform(string name, QueryArguments arguments)
    {
        _performer = _provider.Performers.Single(performer => performer.Name == name);
        _result = await _performer.Perform(new QueryContext(_performer.FullyQualifiedName, CorrelationId.NotSet, Paging.NotPaged, Sorting.None, arguments));
    }
}
