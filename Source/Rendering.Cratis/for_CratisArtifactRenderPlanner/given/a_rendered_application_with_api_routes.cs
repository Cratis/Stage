// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Screenplay.Api;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_rendered_application_with_api_routes : Specification
{
    protected Assembly _assembly = null!;
    protected ApiDescription _description = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var model = invoice_model.Compile(api_routes_model.Source);
        _description = ApiDescription.For(model, ApiConventions.Cratis);
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        _plan = CratisRendering.Plan(model, execution.Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("RouteApp", "RouteApp"));
        Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
        _assembly = RenderedOutput.Load(member_collision_model.Files(_plan));
    }

    protected ArcOptions ScaffoldOptions()
    {
        var settings = _plan.Artifacts.Single(artifact => artifact.RelativePath == "appsettings.json");
        using var document = JsonDocument.Parse(settings.Bytes.AsMemory());
        return JsonSerializer.Deserialize<ArcOptions>(document.RootElement.GetProperty("Cratis").GetProperty("Arc").GetRawText())!;
    }

    // Map the compiled output with Arc itself, using the emitted settings rather than a second route formula.
    protected IReadOnlyList<(string Method, string Route)> Routes(ArcOptions options)
    {
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns([.. _assembly.GetTypes().Where(type => type.IsDefined(typeof(CommandAttribute)))
            .Select(type => new ModelBoundCommandHandler(type, type.GetMethod("Handle")!))]);
        var queries = Substitute.For<IQueryPerformerProviders>();
        queries.Performers.Returns(QueryPerformers());
        var readers = Substitute.For<IInstancesOf<IQueryRequestReader>>();
        readers.GetEnumerator().Returns(_ => new IQueryRequestReader[] { new QueryStringQueryRequestReader() }.AsEnumerable().GetEnumerator());
        using var services = new ServiceCollection()
            .AddSingleton(Options.Create(options))
            .AddSingleton(handlers)
            .AddSingleton(queries)
            .AddSingleton(readers)
            .BuildServiceProvider();
        var mapper = new recording_api_routes();
        mapper.MapCommandEndpoints(services);
        mapper.MapQueryEndpoints(services);
        return mapper.Routes;
    }

    IEnumerable<IQueryPerformer> QueryPerformers() => _assembly.GetTypes()
        .Where(type => type.IsDefined(typeof(ReadModelAttribute)))
        .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => new ModelBoundQueryPerformer(
                type,
                type.FullName!,
                method,
                Substitute.For<IServiceProviderIsService>(),
                Substitute.For<IAuthorizationEvaluator>())));
}
#endif
