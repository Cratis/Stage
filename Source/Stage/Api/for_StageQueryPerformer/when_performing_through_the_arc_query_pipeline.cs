// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.Filters;
using Cratis.Arc.Validation;
using Cratis.Execution;
using Cratis.Specifications;
using Cratis.Stage.Api.for_StageQueryPerformer.given;
using Cratis.Traces;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Api.for_StageQueryPerformer;

public class when_performing_through_the_arc_query_pipeline : a_stage_query_performer
{
    readonly ActivitySource _activitySource = new("StageQueryPerformerSpecs");
    TrackingQueryPerformer _trackingPerformer = null!;
    IQueryRenderers _renderers = null!;
    QueryPipeline _pipeline = null!;
    QueryResult _result = null!;

    void Establish()
    {
        _trackingPerformer = new(_performer);
        var providers = new SinglePerformerProvider(_trackingPerformer);
        var authorization = new AuthorizationFilter(providers);
        var filters = Substitute.For<IQueryFilters>();
        filters.OnPerform(Arg.Any<QueryContext>()).Returns(call => authorization.OnPerform(call.Arg<QueryContext>()));

        var correlationIdAccessor = Substitute.For<ICorrelationIdAccessor>();
        correlationIdAccessor.Current.Returns(CorrelationId.New());
        _renderers = Substitute.For<IQueryRenderers>();
        var activitySource = Substitute.For<IActivitySource<QueryPipeline>>();
        activitySource.ActualSource.Returns(_activitySource);
        _pipeline = new QueryPipeline(
            correlationIdAccessor,
            Substitute.For<IQueryContextManager>(),
            filters,
            providers,
            _renderers,
            Substitute.For<IReadModelInterceptors>(),
            Substitute.For<IDiscoverableValidators>(),
            activitySource);
    }

    async Task Because() =>
        _result = await _pipeline.Perform(
            _performer.FullyQualifiedName,
            QueryArguments.Empty,
            Paging.NotPaged,
            Sorting.None,
            Substitute.For<IServiceProvider>());

    void Destroy() => _activitySource.Dispose();

    [Fact] void should_deny_the_query() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_execute_the_performer() => _trackingPerformer.WasPerformed.ShouldBeFalse();
    [Fact] void should_not_expose_data() => _result.Data.ShouldBeNull();
    [Fact] void should_not_render_any_performer_result() =>
        _renderers.DidNotReceive().Render(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<object>(), Arg.Any<IServiceProvider>());

    sealed class SinglePerformerProvider(IQueryPerformer stagePerformer) : IQueryPerformerProviders
    {
        public IEnumerable<IQueryPerformer> Performers => [stagePerformer];

        public bool TryGetPerformersFor(
            FullyQualifiedQueryName query,
            [NotNullWhen(true)] out IQueryPerformer? performer)
        {
            performer = query == stagePerformer.FullyQualifiedName ? stagePerformer : null;

            return performer is not null;
        }
    }

    sealed class TrackingQueryPerformer(IQueryPerformer performer) : IQueryPerformer
    {
        public QueryName Name => performer.Name;
        public FullyQualifiedQueryName FullyQualifiedName => performer.FullyQualifiedName;
        public Type Type => performer.Type;
        public Type ReadModelType => performer.ReadModelType;
        public IEnumerable<string> Location => performer.Location;
        public string? CustomRoute => performer.CustomRoute;
        public IEnumerable<Type> Dependencies => performer.Dependencies;
        public QueryParameters Parameters => performer.Parameters;
        public bool AllowsAnonymousAccess => performer.AllowsAnonymousAccess;
        public bool SupportsPaging => performer.SupportsPaging;
        public bool WasPerformed { get; private set; }

        public bool IsAuthorized(QueryContext context) => performer.IsAuthorized(context);

        public ValueTask<object?> Perform(QueryContext context)
        {
            WasPerformed = true;

            return performer.Perform(context);
        }
    }
}
