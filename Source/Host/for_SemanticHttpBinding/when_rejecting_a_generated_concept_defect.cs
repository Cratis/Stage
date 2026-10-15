// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Host.for_StageEndpointMapper.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHttpBinding;

public class when_rejecting_a_generated_concept_defect : a_routed_model
{
    IAppendSemanticFacts _facts = null!;
    JsonElement _result;
    protected override TimeProvider Clock => new FixedClock();

    void Establish()
    {
        var plan = compiled_plan.From("""
            concept Code : String
              generate ulid case lower
              validate
                matches "^(?!01)[0-7][0-9a-hjkmnp-tv-z]{25}$"
            module M
              feature F
                slice StateChange S
                  command C
                    code Code generated
                    returns code
            """);
        _facts = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)_facts).Tail().Returns(ulong.MaxValue);

        // The legacy model supplies no read models; the semantic plan owns command binding and execution.
        var structural = EventModelLoader.LoadFromSource("module M\n  feature F\n    slice StateChange S\n      command C");
        MapSemanticModel(structural, plan, _facts);
    }

    async Task Because()
    {
        var response = await Request("POST", "/api/m/f/s/c");
        using var document = JsonDocument.Parse(response.Body);
        _result = document.RootElement.Clone();
    }

    [Fact] void should_report_a_server_exception() => _result.GetProperty("hasExceptions").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_report_input_validation() => _result.GetProperty("validationResults").GetArrayLength().ShouldEqual(0);
    [Fact] void should_not_append_any_facts() => _facts.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());

    sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);
    }
}
