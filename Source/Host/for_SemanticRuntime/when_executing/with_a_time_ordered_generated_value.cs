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

public class with_a_time_ordered_generated_value : Specification
{
    readonly DateTimeOffset _receipt = new(2026, 1, 2, 3, 4, 5, 678, TimeSpan.Zero);
    ISemanticRuntime _runtime = null!;
    SemanticExecutionPlan _plan = null!;
    SemanticAccepted _result = null!;
    SemanticCommandOccurrence _appendedOccurrence = null!;

    void Establish()
    {
        _plan = compiled_plan.From("""
            concept OrderId : String
              generate typeid "order"
            module Orders
              feature Placement
                slice StateChange Place
                  command PlaceOrder
                    orderId OrderId generated identifier
                    produces event OrderPlaced
                      orderId OrderId = orderId
                      placedAt DateTime = $context.occurred
                    returns orderId
            """);
        var appender = Substitute.For<IAppendSemanticFacts, ISemanticFactTail>();
        ((ISemanticFactTail)appender).Tail().Returns(ulong.MaxValue);
        appender.Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Do<SemanticCommandOccurrence>(value => _appendedOccurrence = value)).Returns(Task.CompletedTask);
        _runtime = SemanticRuntimeHosting.Create(_plan, appender);
    }

    async Task Because() => _result = (SemanticAccepted)await _runtime.Execute(_plan.Commands.Values.Single(), new Dictionary<string, JsonElement>(), new ClaimsPrincipal(), new(_receipt, "", "", ""), false);
    void Destroy() => ((IDisposable)_runtime).Dispose();

    [Fact] void should_encode_the_appended_occurrence_in_the_identifier() => IdentifierTime().ShouldEqual(_appendedOccurrence.Occurred.ToUnixTimeMilliseconds());
    [Fact] void should_use_the_dispatch_receipt() => _appendedOccurrence.Occurred.ShouldEqual(_receipt);
    [Fact] void should_map_the_same_occurrence_into_the_fact() => DateTimeOffset.Parse(((SemanticTextValue)_result.Facts.Single().Values.Single(value => value.TargetProperty == _plan.Events.Values.Single().Properties.Single(property => property.Name == "placedAt").Id).Value).Value).ShouldEqual(_receipt);

    long IdentifierTime() => ((SemanticTextValue)_result.Facts.Single().Destination).Value[6..16].Aggregate(0L, (time, symbol) => (time << 5) + "0123456789abcdefghjkmnpqrstvwxyz".IndexOf(symbol));
}
