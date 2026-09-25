// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Arc.Validation;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_append_definitely_rejects : a_semantic_runtime
{
    Exception? _error;
    SemanticExecutionResult _next = null!;

    void Establish() => ((IAppendSemanticFacts)_appender)
        .Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>(), Arg.Any<ulong>())
        .Returns(Task.FromException(new ProducedEventConstraintRejected(ValidationResult.Error("Duplicate project."))));

    async Task Because()
    {
        var payload = new Dictionary<string, JsonElement>
        {
            ["projectId"] = JsonSerializer.SerializeToElement("3fa85f64-5717-4562-b3fc-2c963f66afa6"),
            ["name"] = JsonSerializer.SerializeToElement("Screenplay")
        };
        var occurrence = new SemanticCommandOccurrence(DateTimeOffset.UtcNow, "subject", "name", "user");
        _error = await Catch.Exception(() => _runtime.Execute(_command, payload, new ClaimsPrincipal(), occurrence, false));
        _next = await _runtime.Execute(_command, payload, new ClaimsPrincipal(), occurrence, true);
    }

    [Fact] void should_propagate_the_definite_rejection() => _error.ShouldBeOfExactType<ProducedEventConstraintRejected>();
    [Fact] void should_leave_the_runtime_usable() => _next.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_not_fault_the_world() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldBeNull();
}
