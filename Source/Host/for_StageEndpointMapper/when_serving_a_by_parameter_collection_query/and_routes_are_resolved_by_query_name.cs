// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper.when_serving_a_by_parameter_collection_query;

public class and_routes_are_resolved_by_query_name : given.a_comment_thread
{
    IReadOnlyDictionary<string, string> _queryRoutes = null!;

    void Because() => _queryRoutes = StageSceneRoutes.RoutesByName(new CompositeEndpointDataSource(((IEndpointRouteBuilder)_app).DataSources), "GET");

    [Fact] void should_resolve_the_modeled_query_to_its_keyed_route() => _queryRoutes["CommentsForWorkItem"].ShouldEqual(LegacyKeyedRoute);
    [Fact] void should_not_resolve_the_modeled_query_to_the_collection_route() => _queryRoutes["CommentsForWorkItem"].ShouldNotEqual(CollectionRoute);
}
