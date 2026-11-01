// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_replaying_a_locally_unadmitted_projection_suffix : a_locally_unadmitted_projection
{
    SemanticRuntime _runtime = null!;
    SemanticExecutionResult? _result;
    Exception? _failure;
    given.a_semantic_fact_log _log = null!;

    async Task Because()
    {
        _log = new();
        _runtime = _log.Runtime(_plan, SemanticWorld.Empty);
        _log.Facts.Add(_storedFact);
        var properties = new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6")
        };
        _failure = await Record.ExceptionAsync(async () => _result = await _runtime.Execute(
            Command("TouchProject"),
            properties,
            new ClaimsPrincipal(),
            new(DateTimeOffset.UnixEpoch, "owner", "Owner", "owner"),
            false));
    }

    void Destroy() => _runtime.Dispose();

    [Fact] void should_not_fault_the_runtime() => Assert.Null(_failure);
    [Fact] void should_keep_the_runtime_healthy() => Assert.Null(_runtime.FaultReason);
    [Fact] void should_execute_the_supported_command() => Assert.IsType<SemanticAccepted>(_result);
    [Fact] void should_retain_both_facts() => Assert.Equal(2, Assert.IsType<SemanticAccepted>(_result).World.Facts.Length);
    [Fact] void should_append_at_the_refreshed_tail() => Assert.Equal(0UL, Assert.Single(_log.Attempts).Tail);
    [Fact] async Task should_build_the_supported_projection() => Assert.Single(await _runtime.ReadModels(_readModel.Id));
    [Fact] async Task should_not_materialize_the_unadmitted_projection() => Assert.Empty(await _runtime.ReadModels(_plan.ReadModels.Values.Single(model => model.Name == "RefusedSummary").Id));
    [Fact] void should_append_only_the_new_fact() => Assert.Equal(1, _log.Writes);
    [Fact] void should_still_refuse_the_unsupported_read_model() => Assert.NotNull(SemanticPlanAdmission.Check(_runtime.Plan, _plan.ReadModels.Values.Single(model => model.Name == "RefusedSummary").Id));
}
