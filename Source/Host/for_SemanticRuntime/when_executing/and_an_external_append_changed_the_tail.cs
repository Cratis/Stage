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

public class and_an_external_append_changed_the_tail : a_semantic_runtime
{
    SemanticExecutionResult _result = null!;

    void Establish() => ((ISemanticFactTail)_appender).Tail().Returns(0UL);

    async Task Because() => _result = await _runtime.Execute(
        _command,
        new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["name"] = JsonSerializer.SerializeToElement("Screenplay")
        },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"),
        false);

    [Fact] void should_fault_the_runtime() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_explain_the_external_change() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldContain("outside this session");
    [Fact] void should_not_append_a_fact() => ((IAppendSemanticFacts)_appender).DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>(), Arg.Any<ulong>());
}
