// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_planning_routes : a_semantic_runtime
{
    StageHttpSurface _semantic = null!;
    StageHttpSurface _eventModel = null!;

    void Because()
    {
        _semantic = StageHttpSurface.Create(_runtime.Plan.Model);
        _eventModel = StageHttpSurface.Create(EventModelLoader.LoadFromSource(Source));
    }

    [Fact] void should_keep_eventmodel_command_routes() => _semantic.Operations.Where(operation => operation.Kind == "Execute" || operation.Kind == "Validate")
        .Select(operation => operation.CanonicalPath).ShouldContainOnly(_eventModel.Operations.Where(operation => operation.Kind == "Execute" || operation.Kind == "Validate").Select(operation => operation.CanonicalPath));
    [Fact] void should_add_the_keyed_query_route() => _semantic.Operations.Any(operation => operation.Kind == "QueryKeyed" && operation.CanonicalPath.EndsWith("/project-by-id", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
}
