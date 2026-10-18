// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticReactionSpecificationRenderer
{
    static void RenderProductionExpectations(CSharpCodeBuilder builder, SemanticSpecification specification, SemanticReactionSpecificationPrelude prelude, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        var produces = prelude.Selection.Trigger.Produces;
        foreach (var (expected, index) in specification.ThenEvents.Select((expected, index) => (expected, index)))
        {
            var fields = context.Events[expected.EventContract].Properties.Select(property =>
                (property, expected.Values.Single(value => value.TargetProperty == property.Id).Value));
            RenderExpectedCollections(builder, fields, ProductionValues(types, index));
        }
        builder.Line($"[global::Xunit.FactAttribute] void should_produce_exactly_the_expected_events() => _reactor.ProducedEvents.Count.ShouldEqual({specification.ThenEvents.Length});");
        if (specification.ThenNoEvents) return;
        foreach (var (produced, index) in produces.Select((produced, index) => (produced, index)))
        {
            var identity = produced.Destination is { } destination
                ? new SemanticEventSourceIdentity(produced.DestinationType!, SemanticReactionSpecification.Evaluate(destination, prelude.Append)) : prelude.Append.EventSource!;
            var source = types.EventSourceExpression(types.Value(identity.Value, identity.Type), identity.Type);
            builder.Line($"[global::Xunit.FactAttribute] void should_preserve_destination_{index + 1}() => _reactor.ProducedEvents[{index}].EventSourceId.ShouldEqual((global::Cratis.Chronicle.Events.EventSourceId){source});")
                .Line($"[global::Xunit.FactAttribute] void should_preserve_source_type_{index + 1}() => _reactor.ProducedEvents[{index}].EventSourceType.ShouldEqual(global::Cratis.Chronicle.Events.EventSourceType.Default);")
                .Line($"[global::Xunit.FactAttribute] void should_preserve_stream_type_{index + 1}() => _reactor.ProducedEvents[{index}].EventStreamType.ShouldEqual(global::Cratis.Chronicle.Events.EventStreamType.All);")
                .Line($"[global::Xunit.FactAttribute] void should_preserve_stream_id_{index + 1}() => _reactor.ProducedEvents[{index}].EventStreamId.ShouldEqual((global::Cratis.Chronicle.Events.EventStreamId)global::Cratis.Chronicle.Events.EventStreamId.Default);");
        }
        if (specification.ThenEventsInAnyOrder)
        {
            RenderUnorderedProductions(builder, specification, context, types);
            return;
        }
        foreach (var (expected, index) in specification.ThenEvents.Select((expected, index) => (expected, index)))
        {
            var entry = $"_reactor.ProducedEvents[{index}]";
            var contract = context.Events[expected.EventContract];
            builder.Line($"[global::Xunit.FactAttribute] void should_produce_event_at_position_{index + 1}() => ({entry}.Event is {types.EventType(contract)}).ShouldBeTrue();");
            if (expected.EventSource is { } identity)
            {
                builder.Line($"[global::Xunit.FactAttribute] void should_produce_event_{index + 1}_from_source() => {entry}.EventSourceId.ShouldEqual((global::Cratis.Chronicle.Events.EventSourceId){types.EventSourceExpression(types.Value(identity.Value, identity.Type), identity.Type)});");
            }
            foreach (var property in contract.Properties)
            {
                var value = expected.Values.Single(value => value.TargetProperty == property.Id).Value;
                builder.Line($"[global::Xunit.FactAttribute] void should_produce_event_{index + 1}_with_{Identifiers.ToSnakeCase(property.Name)}() => ({entry}.Event is {types.EventType(contract)} @event && {ValuePredicate(property, value, ProductionValues(types, index))}).ShouldBeTrue();");
            }
        }
    }

    static ValueScope ProductionValues(SemanticTypeSystem types, int index) => new(types, "@event", $"_expected_reaction_event_{index}");

    static string ProductionPredicate(SemanticSpecificationEvent expected, SemanticApplicationContext context, ValueScope scope, string entry)
    {
        var types = scope.Types;
        var @event = context.Events[expected.EventContract];
        var predicates = @event.Properties.Select(property => ValuePredicate(property, expected.Values.Single(value => value.TargetProperty == property.Id).Value, scope));
        var source = expected.EventSource is { } identity ? $" && {entry}.EventSourceId == {types.EventSourceExpression(types.Value(identity.Value, identity.Type), identity.Type)}" : string.Empty;

        return $"{entry}.Event is {types.EventType(@event)} @event{string.Concat(predicates.Select(predicate => $" && {predicate}"))}{source}";
    }

    static void RenderUnorderedProductions(CSharpCodeBuilder builder, SemanticSpecification specification, SemanticApplicationContext context, SemanticTypeSystem types)
    {
        builder.OpenBlock("[global::Xunit.FactAttribute] void should_produce_the_expected_event_multiset()")
            .Line("var actual = _reactor.ProducedEvents.ToArray();")
            .Line($"actual.Length.ShouldEqual({specification.ThenEvents.Length});")
            .Line("var assignments = global::System.Linq.Enumerable.Repeat(-1, actual.Length).ToArray();")
            .OpenBlock($"for (var expectation = 0; expectation < {specification.ThenEvents.Length}; expectation++)")
            .Line("Assign(expectation, new bool[actual.Length]).ShouldBeTrue();").EndBlock().BlankLine()
            .OpenBlock("bool Assign(int expectation, bool[] visited)")
            .OpenBlock("for (var fact = 0; fact < actual.Length; fact++)")
            .Line("if (visited[fact] || !Matches(expectation, fact)) continue;")
            .Line("visited[fact] = true;")
            .OpenBlock("if (assignments[fact] < 0 || Assign(assignments[fact], visited))")
            .Line("assignments[fact] = expectation;").Line("return true;")
            .EndBlock().EndBlock().Line("return false;").EndBlock().BlankLine()
            .OpenBlock("bool Matches(int expectation, int fact)")
            .Line("var entry = actual[fact];").OpenBlock("return expectation switch");
        foreach (var (expected, index) in specification.ThenEvents.Select((expected, index) => (expected, index)))
        {
            builder.Line($"{index} => {ProductionPredicate(expected, context, ProductionValues(types, index), "entry")},");
        }
        builder.Line("_ => false").EndBlock().Line(";").EndBlock().EndBlock();
    }
}
