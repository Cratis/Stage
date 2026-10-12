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

public class with_generated_values_and_a_response : Specification
{
    ISemanticRuntime _runtime = null!;
    SemanticExecutionPlan _plan = null!;
    SemanticAccepted _result = null!;

    void Establish()
    {
        _plan = compiled_plan.From("""
            concept ProjectId : Uuid
            module Projects
              feature Registration
                slice StateChange Register
                  command Register
                    projectId ProjectId generated identifier
                    name String
                    produces event Registered
                      projectId ProjectId = projectId
                      name String = name
                    returns projectId
            """);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        _runtime = SemanticRuntimeHosting.Create(_plan, appender);
    }

    async Task Because() => _result = (SemanticAccepted)await _runtime.Execute(
        _plan.Commands.Values.Single(),
        new Dictionary<string, JsonElement> { ["name"] = JsonSerializer.SerializeToElement("Apollo") },
        new ClaimsPrincipal(),
        new(DateTimeOffset.UtcNow, "subject", "name", "user"),
        false);

    void Destroy() => ((IDisposable)_runtime).Dispose();

    [Fact] void should_generate_a_nonempty_uuid() => Guid.Parse(((SemanticTextValue)((SemanticScalarExecutionResponse)_result.Response!).Value).Value).ShouldNotEqual(Guid.Empty);
    [Fact] void should_append_on_the_returned_identifier() => _result.Facts.Single().Destination.ShouldEqual(((SemanticScalarExecutionResponse)_result.Response!).Value);
    [Fact] void should_map_the_generated_value_into_the_event() => _result.Facts.Single().Values.Select(value => value.Value).ShouldContain(((SemanticScalarExecutionResponse)_result.Response!).Value);
}
