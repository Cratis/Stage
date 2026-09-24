// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_querying;

public class after_a_command : a_semantic_runtime
{
    SemanticExecutionResult _found = null!;
    SemanticExecutionResult _missing = null!;

    async Task Because()
    {
        var principal = new ClaimsPrincipal();
        await _runtime.Execute(
            _command,
            new Dictionary<string, JsonElement>
            {
                ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
                ["name"] = JsonSerializer.SerializeToElement("Screenplay")
            },
            principal,
            new(DateTimeOffset.UtcNow, "subject", "name", "user"),
            false);
        var query = _runtime.Plan.Queries.Values.Single();
        _found = await _runtime.Query(query, SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"), principal);
        _missing = await _runtime.Query(query, SemanticValue.Text("1fa85f64-5717-4562-b3fc-2c963f66afa6"), principal);
    }

    [Fact] void should_find_the_keyed_instance() => ((SemanticAccepted)_found).Queries.Single().Results.Length.ShouldEqual(1);
    [Fact] void should_not_find_another_identity() => ((SemanticAccepted)_missing).Queries.Single().Results.ShouldBeEmpty();
}
