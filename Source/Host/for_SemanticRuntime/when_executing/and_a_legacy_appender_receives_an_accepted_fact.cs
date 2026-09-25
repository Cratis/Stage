// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_a_legacy_appender_receives_an_accepted_fact : a_semantic_runtime
{
    SemanticExecutionResult _first = null!;
    SemanticExecutionResult _second = null!;

    void Establish()
    {
        var tail = ulong.MaxValue;
        ((ISemanticFactTail)_appender).Tail().Returns(_ => tail);
        ((IAppendSemanticFacts)_appender).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>())
            .Returns(_ =>
            {
                // A legacy implementation has no atomic guard; the next preflight detects an external append.
                tail = 1;
                return Task.CompletedTask;
            });
    }

    async Task Because()
    {
        _first = await Execute("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        _second = await Execute("3fa85f64-5717-4562-b3fc-2c963f66afa7");
    }

    [Fact] void should_accept_the_first_command_using_the_original_two_argument_method() => _first.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_detect_the_external_append_on_the_next_command() => _second.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_explain_the_later_tail_change() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldContain("outside this session (0 -> 1)");
    [Fact] async Task should_only_invoke_the_legacy_append_once() => await ((IAppendSemanticFacts)_appender).Received(1).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());

    Task<SemanticExecutionResult> Execute(string id) => _runtime.Execute(
        _command,
        new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement(id),
            ["name"] = JsonSerializer.SerializeToElement("Screenplay")
        },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"),
        false);
}
