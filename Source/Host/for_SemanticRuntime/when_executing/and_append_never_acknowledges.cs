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

public class and_append_never_acknowledges : a_semantic_runtime
{
    SemanticExecutionResult _result = null!;

    void Establish()
    {
        var acknowledgment = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ((IAppendSemanticFacts)_appender).Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>(), Arg.Any<ulong>())
            .Returns(acknowledgment.Task);
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

    [Fact] void should_fault_even_if_the_tail_has_not_yet_advanced() => _result.ShouldBeOfExactType<SemanticUnsupported>();
}
