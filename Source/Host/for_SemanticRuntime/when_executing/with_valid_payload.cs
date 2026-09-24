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

public class with_valid_payload : a_semantic_runtime
{
    SemanticExecutionResult _result = null!;

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

    [Fact] void should_accept_the_command() => (_result is SemanticAccepted).ShouldBeTrue();
    [Fact] async Task should_append_the_fact_once() => await ((IAppendSemanticFacts)_appender).Received(1).Append(Arg.Is<IReadOnlyList<SemanticFact>>(facts => facts.Count == 1), Arg.Any<SemanticCommandOccurrence>());
}
