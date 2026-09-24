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

public class and_tail_cannot_be_reread : a_semantic_runtime
{
    SemanticExecutionResult _result = null!;

    void Establish()
    {
        var reads = 0;
        ((ISemanticFactTail)_appender).Tail().Returns(_ => reads++ == 0
            ? Task.FromResult(0UL)
            : Task.FromException<ulong>(new SemanticCommandExecutionFailed("Tail unavailable.")));
        ((IAppendSemanticFacts)_appender).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>())
            .Returns(Task.FromException(new SemanticCommandExecutionFailed("Acknowledgment lost.")));
    }

    async Task Because() => _result = await _runtime.Execute(
        _command,
        new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["name"] = JsonSerializer.SerializeToElement("Screenplay")
        },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UtcNow, "subject", "name", "user"),
        false);

    [Fact] void should_fault_when_the_tail_cannot_be_confirmed() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_include_the_tail_failure_in_status() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldContain("Tail unavailable");
}
