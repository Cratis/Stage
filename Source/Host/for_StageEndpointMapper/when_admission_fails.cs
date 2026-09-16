// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;

namespace Cratis.Stage.Host.for_StageEndpointMapper;

public class when_admission_fails : given.a_routed_model
{
    Exception? _failure;

    void Because() => _failure = Catch.Exception(() => MapModel(RouteModels.Model(RouteModels.Command("Place"), RouteModels.Command("place"))));

    [Fact] void should_fail_with_the_typed_http_admission_error() => _failure.ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_not_create_a_dynamic_type_factory() => _createdTypes.ShouldBeFalse();
    [Fact] void should_not_resolve_command_providers() => _commandProviders.ShouldBeNull();
    [Fact] void should_not_resolve_query_providers() => _queryProviders.ShouldBeNull();
    [Fact] void should_not_build_or_map_an_application() => _app.ShouldBeNull();
    [Fact] void should_not_execute_commands() => _commands.ShouldBeEmpty();
    [Fact] void should_not_execute_queries() => _queries.ShouldBeEmpty();
}
