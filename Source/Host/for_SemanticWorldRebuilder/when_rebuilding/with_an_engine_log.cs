// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_an_engine_log : a_rebuildable_world
{
    SemanticWorld _world = null!;
    SemanticExecutionResult _duplicate = null!;
    SemanticExecutionResult _lookup = null!;
    SemanticExecutionResult _newRegistration = null!;
    IAppendSemanticFacts _appender = null!;

    async Task Because()
    {
        _world = SemanticWorldRebuilder.Create(_plan, [_event], _mirror, 0);
        _appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)_appender).Tail().Returns(0UL);
        var runtime = SemanticRuntimeHosting.Create(_plan, _appender, _world);
        _lookup = await runtime.Query(_plan.Queries.Values.Single(), _commandValues[0].Value, new ClaimsPrincipal());
        var payload = new Dictionary<string, System.Text.Json.JsonElement>
        {
            ["projectId"] = System.Text.Json.JsonSerializer.SerializeToElement("d7772ed1-59ea-429f-8973-8ef5b8c60470"),
            ["name"] = System.Text.Json.JsonSerializer.SerializeToElement("Screenplay")
        };
        _duplicate = await runtime.Execute(_command, payload, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "", "", ""), false);
        payload["name"] = System.Text.Json.JsonSerializer.SerializeToElement("Another");
        _newRegistration = await runtime.Execute(_command, payload, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "", "", ""), false);
    }

    [Fact] void should_restore_the_keyed_read_model() => _world.ReadModels.Single().Key.ShouldEqual(_commandValues[0].Value);
    [Fact] void should_restore_the_fact_context() => _world.Facts.Single().Context!.EventSource.Value.ShouldEqual(_commandValues[0].Value);
    [Fact] void should_return_the_restored_row_for_a_keyed_query() => ((SemanticAccepted)_lookup).Queries.Single().Results.Single().Key.ShouldEqual(_commandValues[0].Value);
    [Fact] void should_reject_a_duplicate_constraint_claim() => ((SemanticRejected)_duplicate).Category.ShouldEqual(SemanticRejectionCategory.Constraint);
    [Fact] void should_accept_a_new_registration() => _newRegistration.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_only_the_new_registration() => _appender.Received(1).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
}
