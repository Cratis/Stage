// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_an_external_append_changed_the_tail : a_semantic_runtime
{
    SemanticExecutionResult _result = null!;

    a_semantic_fact_log _log = null!;

    async Task Establish()
    {
        var outside = await _runtime.Execute(
            _command,
            new Dictionary<string, JsonElement>
            {
                ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa7"),
                ["name"] = JsonSerializer.SerializeToElement("Outside")
            },
            new ClaimsPrincipal(),
            new(DateTimeOffset.UtcNow, "owner", "Owner", "owner"),
            true);
        _log = new();
        _log.Facts.AddRange(((SemanticAccepted)outside).Facts);
        _runtime = _log.Runtime(_runtime.Plan, SemanticWorld.Empty);
    }

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

    [Fact] void should_accept_against_the_extended_world() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_keep_the_runtime_usable() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldBeNull();
    [Fact] void should_preserve_the_outside_fact() => _result.World.Facts.Length.ShouldEqual(2);
    [Fact] void should_guard_the_durable_pre_batch_world() => _log.Attempts.Single().Tail.ShouldEqual(0UL);
}
