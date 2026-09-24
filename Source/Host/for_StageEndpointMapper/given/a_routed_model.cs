// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Arc.Http;
using Cratis.Arc.Introspection;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.Filters;
using Cratis.Arc.Tenancy;
using Cratis.Arc.Validation;
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Execution;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Runtime;
using Cratis.Traces;
using Cratis.Types;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Stage.Host.for_StageEndpointMapper.given;

public class a_routed_model : Specification
{
    readonly ActivitySource _activitySource = new("StageHttpRoutingSpecs");
    protected readonly List<CommandContext> _commands = [];
    protected readonly List<QueryContext> _queries = [];
    protected readonly List<string> _readModelRequests = [];
    protected bool _rejectQueries;
    protected readonly List<(Type BoundType, string EventSourceId, IReadOnlyList<ProducedEventPayload> Events)> _appends = [];
    protected WebApplication _app = null!;
    protected ICommandHandlerProviders _commandProviders = null!;
    protected IQueryPerformerProviders _queryProviders = null!;
    protected IQueryRenderers _queryRenderers = null!;
    protected IntrospectionService _introspection = null!;
    protected bool _createdTypes;
    private protected StageHttpSurface _surface = null!;
    RequestDelegate _request = null!;

    protected void MapModel(EventModel model, bool enableQueryHttpMethod = true)
    {
        // Deliberately match Program's ordering. A rejected model cannot reach type construction or mapping.
        var routes = new StageHttpRouteOptions(enableQueryHttpMethod);
        _surface = StageHttpSurface.Create(model, routes);
        var builder = WebApplication.CreateBuilder();
        builder.Services.Configure<ArcOptions>(options => options.GeneratedApis = routes.Canonical);
        StageHttpRouteOptions.AlignIntrospection(builder.Services);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi(options => options.AddDocumentTransformer<StageOnlyOperationsDocumentTransformer>());
        builder.Services.AddSingleton(Substitute.For<IHttpRequestContextAccessor>());
        arc_authorization.Register(builder.Services);
        var correlation = Substitute.For<ICorrelationIdAccessor>();
        correlation.Current.Returns(CorrelationId.New());
        builder.Services.AddSingleton(correlation);
        ConfigureReadModels(builder.Services, model);

        _createdTypes = true;
        var types = new DynamicTypeFactory();
        var identity = Substitute.For<IProvideStageIdentity>();
        identity.Current().Returns(new Dictionary<string, string>());
        var appender = Substitute.For<IAppendProducedEvents>();
        appender.Append(Arg.Any<string>(), Arg.Any<IReadOnlyList<ProducedEventPayload>>(), Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns(call =>
            {
                _appends.Add((_commands[^1].Type, call.Arg<string>(), call.Arg<IReadOnlyList<ProducedEventPayload>>()));
                return Task.FromResult<CommandResult?>(null);
            });
        var tenant = Substitute.For<ITenantIdAccessor>();
        tenant.Current.Returns(TenantId.Default);
        var commands = new StageCommandHandlerProvider([model], [types], [appender], [identity], [tenant]);
        var queries = new StageQueryPerformerProvider([model], [types]);
        _commandProviders = new CommandHandlerProviders(Instances<ICommandHandlerProvider>(commands));
        _queryProviders = new QueryPerformerProviders(Instances<IQueryPerformerProvider>(queries));
        builder.Services.AddSingleton(_commandProviders);
        builder.Services.AddSingleton(_queryProviders);
        builder.Services.AddSingleton(Instances<IQueryRequestReader>(new QueryStringQueryRequestReader(), new BodyQueryRequestReader()));
        builder.Services.AddSingleton(Substitute.For<IObservableQueryHandler>());

        ConfigureCommandPipeline(builder.Services, correlation);
        ConfigureQueryPipeline(builder.Services, correlation);
        _app = builder.Build();
        _introspection = new IntrospectionService(
            _commandProviders,
            _queryProviders,
            _app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>(),
            _app.Services.GetRequiredService<IOptions<ArcOptions>>());
        StageEndpointMapper.Map(_app, _surface);
        _app.MapOpenApi();
        BuildRouting();
    }

    protected RouteEndpoint[] Endpoints() => [.. ((IEndpointRouteBuilder)_app).DataSources
        .SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()];

    protected void BuildRouting()
    {
        // Native selection is essential: invoking RequestDelegate on a chosen endpoint misses ambiguity in routing.
        var pipeline = new ApplicationBuilder(_app.Services);
        pipeline.UseRouting();
        pipeline.UseEndpoints(endpoints =>
        {
            foreach (var source in ((IEndpointRouteBuilder)_app).DataSources)
            {
                endpoints.DataSources.Add(source);
            }
        });
        _request = pipeline.Build();
    }

    protected async Task<(int Status, string Body, string? EndpointName)> Request(string method, string path, string body = "{}")
    {
        await using var requestBody = new MemoryStream(Encoding.UTF8.GetBytes(body));
        await using var responseBody = new MemoryStream();
        await using var scope = _app.Services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var parts = path.Split('?', 2);
        context.Request.Method = method;
        context.Request.Path = parts[0];
        context.Request.QueryString = parts.Length > 1 ? new QueryString($"?{parts[1]}") : QueryString.Empty;
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = requestBody.Length;
        context.Request.Body = requestBody;
        context.Response.Body = responseBody;
        await _request(context);

        return (context.Response.StatusCode, Encoding.UTF8.GetString(responseBody.ToArray()), context.GetEndpoint()?.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
    }

    static IInstancesOf<T> Instances<T>(params T[] instances)
        where T : class
    {
        var result = Substitute.For<IInstancesOf<T>>();
        result.GetEnumerator().Returns(_ => ((IEnumerable<T>)instances).GetEnumerator());
        return result;
    }

    void ConfigureCommandPipeline(IServiceCollection services, ICorrelationIdAccessor correlation)
    {
        var filters = Substitute.For<ICommandFilters>();
        filters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
        {
            var context = call.Arg<CommandContext>();
            _commands.Add(context);
            return Task.FromResult(CommandResult.Success(context.CorrelationId));
        });
        var values = Substitute.For<ICommandContextValuesBuilder>();
        values.Build(Arg.Any<object>()).Returns(CommandContextValues.Empty);
        var resolver = Substitute.For<ICommandHandlerArgumentResolver>();
        resolver.Resolve(Arg.Any<ICommandHandler>(), Arg.Any<CommandContext>(), Arg.Any<IServiceProvider>(), Arg.Any<ValidationResultSeverity?>())
            .Returns(call => new CommandHandlerArgumentResolution([], CommandResult.Success(call.Arg<CommandContext>().CorrelationId)));
        var activity = Substitute.For<IActivitySource<CommandPipeline>>();
        activity.ActualSource.Returns(_activitySource);
        services.AddSingleton<ICommandPipeline>(provider => new CommandPipeline(
            correlation,
            filters,
            _commandProviders,
            Substitute.For<ICommandResponseValueHandlers>(),
            Substitute.For<ICommandContextModifier>(),
            values,
            resolver,
            Instances<ICommandExecutionScope>(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            activity));
    }

    void ConfigureQueryPipeline(IServiceCollection services, ICorrelationIdAccessor correlation)
    {
        var authorization = new AuthorizationFilter(_queryProviders);
        var filters = Substitute.For<IQueryFilters>();
        filters.OnPerform(Arg.Any<QueryContext>()).Returns(call =>
        {
            var context = call.Arg<QueryContext>();
            _queries.Add(context);
            return _rejectQueries ? Task.FromResult(QueryResult.Unauthorized(context.CorrelationId)) : authorization.OnPerform(context);
        });
        var activity = Substitute.For<IActivitySource<QueryPipeline>>();
        activity.ActualSource.Returns(_activitySource);
        _queryRenderers = Substitute.For<IQueryRenderers>();
        _queryRenderers.Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>(), Arg.Any<IServiceProvider>()).Returns(call =>
        {
            var data = call.ArgAt<object>(1);
            var count = data switch
            {
                ICollection collection => collection.Count,
                null => 0,
                _ => 1
            };
            return new QueryRendererResult(count, data!);
        });
        var interceptors = Substitute.For<IReadModelInterceptors>();
        interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call => Task.FromResult(call.Arg<IEnumerable<object>>()));
        services.AddSingleton<IQueryPipeline>(new QueryPipeline(
            correlation,
            Substitute.For<IQueryContextManager>(),
            filters,
            _queryProviders,
            _queryRenderers,
            interceptors,
            Substitute.For<IDiscoverableValidators>(),
            activity));
    }

    void ConfigureReadModels(IServiceCollection services, EventModel model)
    {
        var documents = StageModelWalker.Slices(model)
            .Where(located => located.Slice.ReadModel is not null)
            .Select(located => located.Slice.ReadModel!.Id.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(id => id, id => JsonSerializer.Serialize(new { Id = "11111111-1111-1111-1111-111111111111", owner = id }), StringComparer.Ordinal);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var chronicleServices = Substitute.For<IServices>();
        ((IChronicleServicesAccessor)connection).Services.Returns(chronicleServices);
        chronicleServices.ReadModels.GetInstances(Arg.Any<GetInstancesRequest>()).Returns(call =>
        {
            var request = call.Arg<GetInstancesRequest>();
            _readModelRequests.Add(request.ReadModel);
            return new GetInstancesResponse { Instances = [documents[request.ReadModel]], TotalCount = 1 };
        });
        var store = Substitute.For<IEventStore>();
        store.Name.Returns(new EventStoreName("routing-specs"));
        store.Connection.Returns(connection);
        var client = Substitute.For<IChronicleClient>();
        client.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
        services.AddSingleton(client);
        services.AddSingleton<StageEventStoreName>("routing-specs");
    }

    async Task Destroy()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
        _activitySource.Dispose();
    }
}
