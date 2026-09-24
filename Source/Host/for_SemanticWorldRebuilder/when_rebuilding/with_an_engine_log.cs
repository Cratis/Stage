// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_an_engine_log : a_rebuildable_world
{
    SemanticWorld _world = null!;
    SemanticExecutionResult _duplicate = null!;

    async Task Because()
    {
        _world = SemanticWorldRebuilder.Create(_plan, [_event], _mirror, 0);
        var appender = Substitute.For<IAppendSemanticFacts>();
        var runtime = SemanticRuntimeHosting.Create(_plan, appender, _world);
        var payload = new Dictionary<string, System.Text.Json.JsonElement>
        {
            ["projectId"] = System.Text.Json.JsonSerializer.SerializeToElement("d7772ed1-59ea-429f-8973-8ef5b8c60470"),
            ["name"] = System.Text.Json.JsonSerializer.SerializeToElement("Screenplay")
        };
        _duplicate = await runtime.Execute(_command, payload, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "", "", ""), false);
    }

    [Fact] void should_restore_the_keyed_read_model() => _world.ReadModels.Single().Key.ShouldEqual(_commandValues[0].Value);
    [Fact] void should_restore_the_fact_context() => _world.Facts.Single().Context!.EventSource.Value.ShouldEqual(_commandValues[0].Value);
    [Fact] void should_reject_a_duplicate_constraint_claim() => ((SemanticRejected)_duplicate).Category.ShouldEqual(SemanticRejectionCategory.Constraint);
}
