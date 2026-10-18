// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_StageRoutes;

public class when_describing_live_and_snapshot_queries : for_StageEndpointMapper.given.a_routed_model
{
    StageRoutes _routes = null!;

    void Because()
    {
        var plan = compiled_plan.From(Source);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        MapSemanticModel(EventModelLoader.LoadFromSource(Source), plan, appender);
        var scene = new SceneApplication([], [], [], [], [], []);
        _routes = StageRoutes.For(new StageSceneRoutes(scene, _app.Services, NullLogger.Instance), plan.Model);
    }

    [Fact] void should_mark_the_live_query_route() => _routes.Queries["LiveProjects"].EndsWith("?waitForFirstResult=true").ShouldBeTrue();
    [Fact] void should_leave_the_snapshot_query_route_unmarked() => _routes.Queries["AllProjects"].ShouldNotContain("waitForFirstResult");

    const string Source = """
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
              query LiveProjects => observable ProjectSummary[]
              query AllProjects => ProjectSummary[]
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  projectId = projectId
                  name = name
        """;
}
