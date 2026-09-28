// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.Comparison;

internal static class SemanticRunProjections
{
    internal static IReadOnlyList<Projected> Execute(
        SemanticExecutionPlan plan,
        SemanticSpecification specification,
        SemanticRunContext context,
        SemanticRuntimeTypes types,
        Defaults defaults)
    {
        var requested = specification.ThenReadModels.Select(state => state.ReadModel)
            .Concat(specification.ThenQueries.Select(query => plan.Queries[query.Query].ReadModel))
            .Concat(plan.Projections.Values.Where(projection => context.Facts.Any(fact =>
                projection.Scope?.From.Any(from => from.EventContract == fact.EventContract) == true ||
                projection.Transitions.Any(transition => transition.EventContract == fact.EventContract))).Select(projection => projection.ReadModel)).Distinct();
        var output = new List<Projected>();
        foreach (var id in requested)
        {
            var model = plan.ReadModels[id];
            var projection = plan.Projections.Values.Single(value => value.ReadModel == id);
            var method = typeof(SemanticRunProjections).GetMethod(nameof(Define), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(types.ForReadModel(model));
            output.AddRange((IReadOnlyList<Projected>)method.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [plan, projection, model, context, types, defaults], null)!);
        }
        return output;
    }

    static List<Projected> Define<TReadModel>(
        SemanticExecutionPlan plan,
        SemanticProjection projection,
        SemanticReadModel model,
        SemanticRunContext context,
        SemanticRuntimeTypes types,
        Defaults defaults)
        where TReadModel : class
    {
        var scenario = new ReadModelScenario<TReadModel>(null, defaults).WithProjection(builder =>
        {
            builder.NoAutoMap();
            if (projection.Scope is { } scope)
            {
                foreach (var from in scope.From)
                {
                    var @event = plan.Events[from.EventContract];
                    var mappings = from.Mappings.Select(mapping => (
                        Target: model.Properties.Single(property => property.Id == mapping.Target[0]).Name,
                        Source: mapping.Source is SemanticProjectionEventProperty source ? @event.Properties.Single(property => property.Id == source.Path[0]).Name : null)).ToArray();
                    DefineFrom(builder, types.For(from.EventContract), mappings);
                }
            }
            else
            {
                foreach (var transition in projection.Transitions)
                {
                    var @event = plan.Events[transition.EventContract];
                    var mappings = transition.Mappings.Select(mapping => (
                        Target: model.Properties.Single(property => property.Id == mapping.TargetProperty).Name,
                        Source: (string?)@event.Properties.Single(property => property.Id == ((SemanticResolvedExpression)mapping.Source).Target).Name)).ToArray();
                    DefineFrom(builder, types.For(transition.EventContract), mappings);
                }
            }
        });
        for (var index = 0; index < context.Facts.Count; index++)
        {
            var fact = context.Facts[index];
            var destination = context.Destinations[index];
            var key = destination switch
            {
                SemanticTextValue text => text.Value,
                SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
                SemanticBooleanValue boolean => boolean.Value.ToString(CultureInfo.InvariantCulture),
                _ => throw new UnsupportedSemanticMapping()
            };
            scenario.CollectEventsFor(new EventSourceId(key), [types.Materialize(fact)]);
        }
        var output = new List<Projected>();
        foreach (var (source, instance) in scenario.Instances)
        {
            // The sink stores the identifier as its document key, not as a field in the materialized model.
            var identifier = model.Properties.Single(property => property.IsIdentifier);
            var key = SemanticValue.Text(source.ToString());
            var values = model.Properties.Select(property => new SemanticPropertyValue(
                property.Id,
                property.Id == identifier.Id ? key : Value(typeof(TReadModel).GetProperty(property.Name)!.GetValue(instance), property.Type, plan)))
                .ToImmutableArray();
            output.Add(new(model.Id, key, values));
        }
        return output;
    }

    static void DefineFrom<TReadModel>(IProjectionBuilderFor<TReadModel> builder, Type eventType, (string Target, string? Source)[] mappings)
    {
        typeof(SemanticRunProjections).GetMethod(nameof(DefineEvent), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TReadModel), eventType).Invoke(null, BindingFlags.DoNotWrapExceptions, null, [builder, mappings], null);
    }

    static void DefineEvent<TReadModel, TEvent>(IProjectionBuilderFor<TReadModel> builder, (string Target, string? Source)[] mappings)
    {
        // The generic parameter is a runtime-emitted type carrying [EventType]; the analyzer cannot see it.
#pragma warning disable CHR0002
        builder.From<TEvent>(from =>
        {
            foreach (var (target, source) in mappings)
            {
                var set = from.Set(new PropertyPath(target));
                if (source is null) set.ToEventSourceId();
                else set.To(new PropertyPath(source));
            }
        });
#pragma warning restore CHR0002
    }

    static SemanticValue Value(object? value, SemanticTypeReference type, SemanticExecutionPlan plan)
    {
        if (value is null) return SemanticValue.Null;
        var primitive = type.Kind == SemanticTypeReferenceKind.Concept
            ? plan.Model.Application.Concepts.Single(concept => concept.Id == type.Target).Primitive : type.Primitive;
        return primitive switch
        {
            SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber => SemanticValue.Number(Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
            SemanticPrimitiveType.Boolean => SemanticValue.Boolean((bool)value),
            SemanticPrimitiveType.Date when value is DateOnly date => SemanticValue.Text(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            SemanticPrimitiveType.DateTime when value is DateTimeOffset time => SemanticValue.Text(time.ToString("O", CultureInfo.InvariantCulture)),
            _ => SemanticValue.Text(value.ToString()!)
        };
    }

    internal sealed record Projected(SemanticId ReadModel, SemanticValue Key, ImmutableArray<SemanticPropertyValue> Values);
}
