// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Cratis.Stage.Semantics;
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
        if (context.Facts.Count == 0 && context.WorldFixture.GivenReadModels.IsEmpty) return [];

        var requested = RequestedReadModels(plan, specification, context.WorldFixture, context.Facts);
        var output = new List<Projected>();
        foreach (var id in requested)
        {
            output.AddRange(ForReadModel(plan, id, context, types, defaults));
        }
        return output;
    }

    internal static IEnumerable<SemanticId> RequestedReadModels(
        SemanticExecutionPlan plan, SemanticSpecification specification, SemanticSpecification fixture, IEnumerable<SemanticSpecificationEvent> facts) =>
        fixture.GivenReadModels.Select(state => state.ReadModel)
            .Concat(specification.ThenReadModels.Select(state => state.ReadModel))
            .Concat(specification.ThenAbsentReadModels.Select(state => state.ReadModel))
            .Concat(specification.ThenQueries.Select(query => plan.Queries[query.Query].ReadModel))
            .Concat(plan.Projections.Values.Where(projection => projection.Target != SemanticProjectionTargetKind.Event && facts.Any(fact => SemanticProjectionObservation.Observes(projection, fact.EventContract)))
                .Select(projection => projection.ReadModel)).Where(plan.ReadModels.ContainsKey).Distinct();

    internal static IReadOnlyList<Projected> ForReadModel(SemanticExecutionPlan plan, SemanticId id, SemanticRunContext context, SemanticRuntimeTypes types, Defaults defaults)
    {
        if (context.Facts.Count == 0 && context.WorldFixture.GivenReadModels.IsEmpty) return [];
        var model = plan.ReadModels[id];
        var projection = plan.Projections.Values.SingleOrDefault(value => value.Target != SemanticProjectionTargetKind.Event && value.ReadModel == id);
        if (projection is null)
        {
            return [.. Seeds(context, id).Select(seed => new Projected(id, seed.Key, seed.Values))];
        }
        var method = typeof(SemanticRunProjections).GetMethod(nameof(Define), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(types.ForReadModel(model));

        try
        {
            return (IReadOnlyList<Projected>)method.Invoke(null, BindingFlags.DoNotWrapExceptions, null, [plan, projection, model, context, types, defaults], null)!;
        }
        catch (SemanticProjectionLoweringRefused exception)
        {
            throw new SemanticProjectionLoweringRefused(new InvalidOperationException($"Projection '{projection.Name}' ({projection.Id}): {exception.Message}", exception))
            {
                Projection = projection.Id
            };
        }
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
        using var scenario = new ReadModelScenario<TReadModel>(null, defaults).WithProjection(builder =>
        {
            builder.NoAutoMap();
            if (projection.Scope is { } scope)
            {
                foreach (var from in scope.From)
                {
                    // A product block whose event this run never delivers cannot fire; Chronicle cannot lower the product.
                    if (from.Mappings.Any(mapping => mapping.Factor is not null) && !context.Facts.Any(fact => fact.EventContract == from.EventContract)) continue;
                    var @event = plan.Events[from.EventContract];
                    var literals = scope.Every?.Mappings.Where(mapping => mapping.Source is SemanticProjectionLiteral).ToArray() ?? [];
                    var mappings = from.Mappings.Where(mapping => mapping.Operation != SemanticProjectionOperation.Set || !literals.Any(literal => literal.Target.SequenceEqual(mapping.Target))).Concat(literals).ToArray();
                    SemanticRunScopedMappings.Define(builder, types.For(from.EventContract), mappings, model, @event, from.Key, plan);
                }
                SemanticRunScopedMappings.DefineRelated(builder, scope, model.Properties, plan, types);
                if (scope.Every is { } every)
                {
                    DefineEvery(builder, every, model);
                }
                foreach (var removal in scope.Removals)
                {
                    var keyProperty = removal.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var id] } }
                        ? plan.Events[removal.EventContract].Properties.Single(property => property.Id == id).Name : null;
                    DefineRemoval(builder, types.For(removal.EventContract), keyProperty);
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
        foreach (var seed in Seeds(context, model.Id))
        {
            var instance = Activator.CreateInstance<TReadModel>();
            foreach (var value in seed.Values)
            {
                var property = typeof(TReadModel).GetProperty(model.Properties.Single(property => property.Id == value.TargetProperty).Name)!;
                property.SetValue(instance, types.ReadModelValue(value.Value, model.Properties.Single(candidate => candidate.Id == value.TargetProperty).Type));
            }
            scenario.CollectReadModelFor(new EventSourceId(((SemanticTextValue)seed.Key).Value), instance);
        }
        CollectEvents(scenario, projection, context, types);
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

    static void CollectEvents<TReadModel>(ReadModelScenario<TReadModel> scenario, SemanticProjection projection, SemanticRunContext context, SemanticRuntimeTypes types)
        where TReadModel : class
    {
        for (var index = 0; index < context.Facts.Count; index++)
        {
            var fact = context.Facts[index];
            if (!SemanticProjectionObservation.Observes(projection, fact.EventContract)) continue;
            var destination = context.Destinations[index];

            // A fixture replaces that key's history, including property-keyed removals.
            // Only action and reaction facts may subsequently change it.
            var projectedKey = projection.Scope?.From.SingleOrDefault(from => from.EventContract == fact.EventContract)?.Key ??
                projection.Scope?.Removals.SingleOrDefault(removal => removal.EventContract == fact.EventContract)?.Key;
            var fixtureKey = projectedKey switch
            {
                SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var property] } } => fact.Values.Single(value => value.TargetProperty == property).Value,
                SemanticProjectionValueKey { Value: SemanticProjectionLiteral literal } => literal.Value,
                _ => destination
            };
            if (index < context.WorldFixture.GivenEvents.Length && Seeds(context, projection.ReadModel)
                .Any(seed => SemanticExpectationComparer.AreEqual(seed.Key, fixtureKey)))
            {
                continue;
            }
            var key = destination switch
            {
                // Match the private log key used for an unsourced semantic append.
                SemanticNullValue => "stage:semantic-null",
                SemanticTextValue text => text.Value,
                SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
                SemanticBooleanValue boolean => boolean.Value.ToString(CultureInfo.InvariantCulture),
                _ => throw new UnsupportedSemanticMapping()
            };
            if (projection.Scope is { } scope && !scope.From.Any(from => from.EventContract == fact.EventContract))
            {
                foreach (var child in scope.Children)
                {
                    if (child.Scope.From.SingleOrDefault(from => from.EventContract == fact.EventContract) is not { } from) continue;
                    var parent = from.ParentKey is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var property] } }
                        ? fact.Values.Single(value => value.TargetProperty == property).Value : destination;
                    if (parent is not SemanticTextValue text || !scenario.Instances.Keys.Any(source => source.ToString() == text.Value))
                        throw new SemanticProjectionLoweringRefused(new NotSupportedException("A child projection needs an existing parent instance before its child event; Chronicle cannot materialize that parent implicitly."));
                }
            }
            scenario.CollectEventsFor(new EventSourceId(key), [types.Materialize(fact)]);
        }
    }

    static IEnumerable<SemanticSpecificationReadModel> Seeds(SemanticRunContext context, SemanticId model) =>
        context.WorldFixture.GivenReadModels.Where(seed => seed.ReadModel == model)
            .GroupBy(seed => SemanticRunContext.Canonical(seed.Key), StringComparer.Ordinal).Select(group => group.Last());

    static void DefineEvery<TReadModel>(IProjectionBuilderFor<TReadModel> builder, SemanticProjectionEvery every, SemanticReadModel model)
    {
        void Define(Action<string> set)
        {
            var identifier = model.Properties.Single(property => property.IsIdentifier);
            if (every.SubscribesToAllEvents && !every.Mappings.Any(mapping => mapping.Target.Contains(identifier.Id)))
            {
                set(identifier.Name);
            }
            foreach (var mapping in every.Mappings.Where(mapping => mapping.Source is not SemanticProjectionLiteral))
            {
                var target = model.Properties.Single(property => property.Id == mapping.Target[0]);
                set(target.Name);
            }
        }
        if (every.SubscribesToAllEvents)
        {
            // FromAll has no child-inclusion switch; child scopes are refused before lowering.
            builder.FromAll(from => Define(target => from.Set(Property<TReadModel>(target)).ToEventSourceId()));
        }
        else
        {
            builder.FromEvery(from =>
            {
                from.ExcludeChildProjections();
                Define(target => from.Set(Property<TReadModel>(target)).ToEventSourceId());
            });
        }
    }

    static Expression<Func<TReadModel, object>> Property<TReadModel>(string name)
    {
        var parameter = Expression.Parameter(typeof(TReadModel), "model");

        return Expression.Lambda<Func<TReadModel, object>>(Expression.Convert(Expression.Property(parameter, name), typeof(object)), parameter);
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

    static void DefineRemoval<TReadModel>(IProjectionBuilderFor<TReadModel> builder, Type eventType, string? keyProperty) =>
        typeof(SemanticRunProjections).GetMethod(nameof(RemoveEvent), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TReadModel), eventType).Invoke(null, BindingFlags.DoNotWrapExceptions, null, [builder, keyProperty], null);

    static void RemoveEvent<TReadModel, TEvent>(IProjectionBuilderFor<TReadModel> builder, string? keyProperty)
    {
        // The generic parameter is a runtime-emitted event contract, as in DefineEvent.
#pragma warning disable CHR0002
        if (keyProperty is null)
        {
            builder.RemovedWith<TEvent>();
        }
        else
        {
            var parameter = Expression.Parameter(typeof(TEvent), "@event");
            var key = Expression.Lambda<Func<TEvent, object>>(Expression.Convert(Expression.Property(parameter, keyProperty), typeof(object)), parameter);
            builder.RemovedWith<TEvent>(removed => removed.UsingKey(key));
        }
#pragma warning restore CHR0002
    }

    static SemanticValue Value(object? value, SemanticTypeReference type, SemanticExecutionPlan plan)
    {
        if (value is null) return type.IsCollection ? SemanticValue.Array([]) : SemanticValue.Null;
        if (type.IsCollection)
        {
            return SemanticValue.Array([.. ((System.Collections.IEnumerable)value).Cast<object>().Select(item => Value(item, type with { IsCollection = false, IsOptional = false }, plan))]);
        }
        if (type.Kind == SemanticTypeReferenceKind.CompositeType)
        {
            var composite = plan.Model.Application.Types.Single(composite => composite.Id == type.Target);
            return SemanticValue.Composite([.. composite.Properties
                .Where(property => !property.Type.IsOptional || value.GetType().GetProperty(property.Name)!.GetValue(value) is not null)
                .Select(property => new SemanticPropertyValue(property.Id, Value(value.GetType().GetProperty(property.Name)!.GetValue(value), property.Type, plan)))]);
        }
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
