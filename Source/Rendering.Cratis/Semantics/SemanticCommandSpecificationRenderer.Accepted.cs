// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// The accepted-command half of <see cref="SemanticCommandSpecificationRenderer"/>.
/// </summary>
internal static partial class SemanticCommandSpecificationRenderer
{
    static void RenderAccepted(SpecificationScope scope)
    {
        var generated = new SemanticGeneratedCommandSpecification(scope.Specification, scope.Command, scope.Types, scope.Context);
        var builder = scope.Builder;
        var specification = scope.Specification;
        builder.Using("Cratis.Arc.Chronicle.Testing.Commands")
            .Using("Cratis.Chronicle.Events");
        foreach (var (expected, index) in Indexed(specification.ThenEvents))
        {
            RenderExpectedCollections(scope, expected, index);
        }

        builder.Line("[global::Xunit.FactAttribute] void should_succeed() => _result.ShouldBeSuccessful();");
        RenderAppendedCount(scope);

        if (ExpectsAnyOrder(scope))
        {
            RenderUnorderedEvents(scope);
            return;
        }

        foreach (var (expected, index) in Indexed(specification.ThenEvents))
        {
            RenderOrderedEvent(scope, expected, index, generated);
        }
    }

    static bool ExpectsAnyOrder(SpecificationScope scope) =>
        scope.Specification.ThenEventsInAnyOrder &&
        (scope.Specification.ThenEvents.Length > 1 || scope.Context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V8));

    static IEnumerable<(T Value, int Index)> Indexed<T>(IEnumerable<T> source) => source.Select((value, index) => (value, index));

    static void RenderExpectedCollections(SpecificationScope scope, SemanticSpecificationEvent expected, int index)
    {
        var context = scope.Context;
        foreach (var property in context.Events[expected.EventContract].Properties.Where(property => property.Type.IsCollection))
        {
            var value = expected.Values.Single(_ => _.TargetProperty == property.Id).Value;
            if (value is not SemanticArrayValue)
            {
                continue;
            }

            var elementType = property.Type with { IsCollection = false, IsOptional = false };
            if (SemanticTypeSystem.DeclarationNeedsCommon(elementType))
            {
                scope.Builder.Using(context.CommonNamespace);
            }

            scope.Builder.Line($"static readonly {scope.Types.Type(elementType)}[] {ExpectedCollectionName(property, index)} = {scope.Types.Value(value, property.Type)};");
        }
    }

    static void RenderAppendedCount(SpecificationScope scope)
    {
        var builder = scope.Builder;
        var count = scope.Specification.ThenEvents.Length;
        if (count == 0 && scope.Command.Produces.Any(produced => produced.When is not null))
        {
            builder.Line(scope.SeedInLog
                ? "[global::Xunit.FactAttribute] void should_append_no_new_events() => (_scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess) - _givenEventCount).ShouldEqual(0);"
                : "[global::Xunit.FactAttribute] void should_append_no_new_events() => _scenario.AppendedEvents.Count.ShouldEqual(0);");
        }
        if (count > 1 || (count == 1 && !scope.SeedInLog && scope.Command.Produces.Any(produced => produced.When is not null)))
        {
            builder.Line(scope.SeedInLog
                ? $"[global::Xunit.FactAttribute] void should_append_exactly_{count}_events() => (_scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess) - _givenEventCount).ShouldEqual({count});"
                : $"[global::Xunit.FactAttribute] void should_append_exactly_{count}_events() => _scenario.AppendedEvents.Count.ShouldEqual({count});");
        }

        if (scope.SeedInLog && count == 1)
        {
            builder.Line("[global::Xunit.FactAttribute] void should_append_exactly_one_new_event() => (_scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess) - _givenEventCount).ShouldEqual(1);");
        }
    }

    static void RenderOrderedEvent(SpecificationScope scope, SemanticSpecificationEvent expected, int index, SemanticGeneratedCommandSpecification generated)
    {
        var builder = scope.Builder;
        var context = scope.Context;
        var types = scope.Types;
        var @event = context.Events[expected.EventContract];
        var produced = SemanticProductionSelection.Taken(scope.Command, scope.Specification)[index];
        if (generated.DestinationNeedsCommon(produced) ||
            PropertyValuesNeedCommon(@event.Properties, expected.Values))
        {
            builder.Using(context.CommonNamespace);
        }

        var predicate = @event.Properties.IsEmpty ? "true" : string.Join(" && ", @event.Properties.Select(property =>
        {
            var value = expected.Values.Single(_ => _.TargetProperty == property.Id).Value;
            return generated.EventPredicate(produced, property, EventPropertyPredicate(property, value, types, index));
        }));
        var sourceValue = generated.DestinationExpression(produced);
        var name = $"should_have_appended_{Identifiers.ToSnakeCase(@event.Name)}";
        if (IsPlainSingleEvent(scope, expected))
        {
            builder.Line($"[global::Xunit.FactAttribute] async global::System.Threading.Tasks.Task {name}() => await _scenario.ShouldHaveAppendedEvent<{types.SliceType(scope.Command.Id, scope.Command.Name)}, {types.EventType(@event)}>({sourceValue}, @event => {predicate});");
            return;
        }

        var appended = scope.SeedInLog
            ? $"_scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).ToArray()[_givenEventCount + {index}]"
            : $"_scenario.AppendedEvents[{index}]";
        var route = SemanticSpecificationRouteRendering.Predicate(expected, context, appended);
        builder.Line($"[global::Xunit.FactAttribute] void {name}_at_position_{index + 1}() => ({appended}.Event.Context.EventSourceId == {sourceValue} && {appended}.Event.Content is {types.EventType(@event)} @event && {predicate}{route}).ShouldBeTrue();");
    }

    static bool IsPlainSingleEvent(SpecificationScope scope, SemanticSpecificationEvent expected) =>
        scope.Specification.ThenEvents.Length == 1 && !scope.SeedInLog && expected.Route is null && !expected.Unrouted;

    static void RenderUnorderedEvents(SpecificationScope scope)
    {
        if (scope.Context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V8))
        {
            RenderAssignedEvents(scope);
            return;
        }

        // Preserve the pre-v8 greedy matcher byte-for-byte.
        var builder = scope.Builder;
        builder.OpenBlock("[global::Xunit.FactAttribute] void should_append_the_expected_event_multiset()")
            .Line(scope.SeedInLog
                ? "var remaining = _scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).Skip(_givenEventCount).ToList();"
                : "var remaining = _scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).ToList();")
            .Line($"remaining.Count.ShouldEqual({scope.Specification.ThenEvents.Length});");
        foreach (var (expected, index) in Indexed(scope.Specification.ThenEvents))
        {
            RenderGreedyMatch(scope, expected, index);
        }

        builder.EndBlock();
    }

    static void RenderGreedyMatch(SpecificationScope scope, SemanticSpecificationEvent expected, int index)
    {
        var builder = scope.Builder;
        var context = scope.Context;
        var types = scope.Types;
        var @event = context.Events[expected.EventContract];
        var eventNamespace = SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(expected.EventContract).Path);
        builder.Using(eventNamespace);
        var predicates = @event.Properties.Select(property =>
        {
            var value = expected.Values.Single(_ => _.TargetProperty == property.Id).Value;
            if (SemanticTypeSystem.ValueNeedsCommon(value, property.Type))
            {
                builder.Using(context.CommonNamespace);
            }

            return EventPropertyPredicate(property, value, types, index);
        });
        var produced = SemanticProductionSelection.Taken(scope.Command, scope.Specification).First(_ => _.EventContract == expected.EventContract);
        var identity = SemanticDestinations.ForSpecification(scope.Specification, scope.Command, produced);
        var source = $" && entry.Event.Context.EventSourceId == {types.EventSourceExpression(types.Value(identity.Value, identity.Type), identity.Type)}";
        if (SemanticTypeSystem.ValueNeedsCommon(identity.Value, identity.Type))
        {
            builder.Using(context.CommonNamespace);
        }

        builder.Line($"var match{index} = remaining.FindIndex(entry => entry.Event.Content is {types.EventType(@event)} @event{source}{string.Concat(predicates.Select(predicate => $" && {predicate}"))});")
            .Line($"(match{index} >= 0).ShouldBeTrue();")
            .Line($"remaining.RemoveAt(match{index});");
    }

    static void RenderAssignedEvents(SpecificationScope scope)
    {
        var builder = scope.Builder;
        var count = scope.Specification.ThenEvents.Length;
        builder.OpenBlock("[global::Xunit.FactAttribute] void should_append_the_expected_event_multiset()")
            .Line(scope.SeedInLog
                ? "var actual = _scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).Skip(_givenEventCount).ToArray();"
                : "var actual = _scenario.AppendedEvents.Where(entry => entry.Result.IsSuccess).ToArray();")
            .Line($"actual.Length.ShouldEqual({count});")
            .Line("var assignments = global::System.Linq.Enumerable.Repeat(-1, actual.Length).ToArray();")
            .OpenBlock($"for (var expectation = 0; expectation < {count}; expectation++)")
            .Line("Assign(expectation, new bool[actual.Length]).ShouldBeTrue();")
            .EndBlock().BlankLine()
            .OpenBlock("bool Assign(int expectation, bool[] visited)")
            .OpenBlock("for (var fact = 0; fact < actual.Length; fact++)")
            .Line("if (visited[fact] || !Matches(expectation, fact)) continue;")
            .Line("visited[fact] = true;")
            .OpenBlock("if (assignments[fact] < 0 || Assign(assignments[fact], visited))")
            .Line("assignments[fact] = expectation;")
            .Line("return true;")
            .EndBlock().EndBlock()
            .Line("return false;")
            .EndBlock().BlankLine()
            .OpenBlock("bool Matches(int expectation, int fact)")
            .Line("var entry = actual[fact];")
            .OpenBlock("return expectation switch");
        foreach (var (expected, index) in Indexed(scope.Specification.ThenEvents))
        {
            RenderAssignedArm(scope, expected, index);
        }

        builder.Line("_ => false")
            .EndBlock().Line(";")
            .EndBlock().EndBlock();
    }

    static void RenderAssignedArm(SpecificationScope scope, SemanticSpecificationEvent expected, int index)
    {
        var context = scope.Context;
        var types = scope.Types;
        var @event = context.Events[expected.EventContract];
        var predicates = @event.Properties.Select(property => EventPropertyPredicate(property, expected.Values.Single(value => value.TargetProperty == property.Id).Value, types, index));
        var sources = new[] { expected.EventSource, scope.Specification.When!.EventSource }.OfType<SemanticEventSourceIdentity>();
        var source = string.Concat(sources.Select(identity =>
            $" && entry.Event.Context.EventSourceId == {types.EventSourceExpression(types.Value(identity.Value, identity.Type), identity.Type)}"));
        if (sources.Any(identity => SemanticTypeSystem.ValueNeedsCommon(identity.Value, identity.Type)) ||
            PropertyValuesNeedCommon(@event.Properties, expected.Values))
        {
            scope.Builder.Using(context.CommonNamespace);
        }

        var route = SemanticSpecificationRouteRendering.Predicate(expected, context, "entry");
        scope.Builder.Line($"{index} => entry.Event.Content is {types.EventType(@event)} @event{source}{string.Concat(predicates.Select(predicate => $" && {predicate}"))}{route},");
    }
}
