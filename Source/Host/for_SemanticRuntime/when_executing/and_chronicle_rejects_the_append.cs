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

public class and_chronicle_rejects_the_append : a_semantic_runtime
{
    Exception? _error;

    void Establish() => ((IAppendSemanticFacts)_appender)
        .Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>())
        .Returns(Task.FromException(new SemanticCommandExecutionFailed("Chronicle refused the append.")));

    async Task Because() => _error = await Catch.Exception(() => _runtime.Execute(
        _command,
        new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["name"] = JsonSerializer.SerializeToElement("Screenplay")
        },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UtcNow, "subject", "name", "user"),
        false));

    [Fact] void should_propagate_the_failure() => _error.ShouldBeOfExactType<SemanticCommandExecutionFailed>();
    [Fact] async Task should_not_commit_a_read_model() => (await _runtime.ReadModels(_runtime.Plan.ReadModels.Keys.Single())).ShouldBeEmpty();
}
