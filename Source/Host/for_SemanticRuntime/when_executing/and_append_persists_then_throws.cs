// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_append_persists_then_throws : a_semantic_runtime
{
    SemanticExecutionResult _first = null!;
    SemanticExecutionResult _next = null!;
    SemanticExecutionResult _query = null!;

    void Establish()
    {
        var tail = 0UL;
        ((ISemanticFactTail)_appender).Tail().Returns(_ => tail);
        ((IAppendSemanticFacts)_appender).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>())
            .Returns(_ =>
            {
                tail++;
                return Task.FromException(new SemanticCommandExecutionFailed("Acknowledgment lost."));
            });
    }

    async Task Because()
    {
        var payload = new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["name"] = JsonSerializer.SerializeToElement("Screenplay")
        };
        var occurrence = new SemanticCommandOccurrence(DateTimeOffset.UtcNow, "subject", "name", "user");
        _first = await _runtime.Execute(_command, payload, new ClaimsPrincipal(), occurrence, false);
        _next = await _runtime.Execute(_command, payload, new ClaimsPrincipal(), occurrence, true);
        _query = await _runtime.Query(_runtime.Plan.Queries.Values.Single(), SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"), new ClaimsPrincipal());
    }

    [Fact] void should_fault_the_original_command() => _first.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_refuse_later_commands() => _next.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_refuse_later_queries() => _query.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_expose_the_fault_reason() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldNotBeNull();
    [Fact] async Task should_not_commit_the_unacknowledged_world() => (await _runtime.ReadModels(_runtime.Plan.ReadModels.Keys.Single())).ShouldBeEmpty();
}
