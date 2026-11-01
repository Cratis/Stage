// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cratis.Stage.Host.for_StageRoutes;

public class when_building_routes_repeatedly : Specification
{
    int _constructed;
    StageRoutes _first = null!;
    StageRoutes _second = null!;

    async Task Because()
    {
        var plan = compiled_plan.From(Source);
        await using var app = WebApplication.CreateBuilder().Build();
        var scene = new Contracts.Scene.SceneApplication([], [], [], [], [], []);
        var source = new StageRoutesSource(
            new StageSceneRoutes(scene, app.Services, NullLogger.Instance),
            plan.Model,
            model =>
            {
                _constructed++;
                return new SemanticListCreation(model);
            });
        _first = source.Build();
        _second = source.Build();
    }

    [Fact] void should_infer_list_creation_once_for_the_model() => _constructed.ShouldEqual(1);
    [Fact] void should_build_the_same_schemas_each_time() => JsonSerializer.Serialize(_second.CommandSchemas).ShouldEqual(JsonSerializer.Serialize(_first.CommandSchemas));

    const string Source = """
        concept OrderId : Uuid
        module Stock
          feature Dispatch
            slice StateChange NoteDelivery
              command NoteDelivery
                orderId OrderId
                note    String
                returns note
        """;
}
