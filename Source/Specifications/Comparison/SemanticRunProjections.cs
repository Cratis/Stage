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
                var appended = context.Facts.Select(fact => fact.EventContract).ToHashSet();
                foreach (var from in scope.From.Where(from => !SemanticProjectionProducts.IsUnreachable(from, appended)))
                {
                    var @event = plan.Events[from.EventContract];

                    SemanticRunScopedMappings.Define(builder, types.For(from.EventContract), FromMappings(scope, from), model, @event, from.Key, plan);
                }
                SemanticRunScopedMappings.DefineRelated(builder, scope, model.Properties, plan, types);
                if (scope.Every is { } every)
                {
                    DefineEvery(builder, scope, every, model);
                }
                foreach (var removal in scope.Removals)
                {
                    var keyProperty = removal.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var id] } }
                        ? plan.Events[removal.EventContract].Properties.Single(property => property.Id == id).Name : null;
                    DefineRemoval(builder, types.For(removal.EventContract), keyProperty, removal.Key, model, plan.Events[removal.EventContract], plan);
                }
            }
            else
            {
                // A flat transition is a root from block keyed by an event property, with each mapping an event property or a literal.
                foreach (var transition in projection.Transitions)
                {
                    var @event = plan.Events[transition.EventContract];
                    var key = new SemanticProjectionValueKey(new SemanticProjectionEventProperty([((SemanticResolvedExpression)transition.AffectedInstance.Key!).Target]));
                    var mappings = transition.Mappings.Select(mapping => new SemanticProjectionMapping([mapping.TargetProperty], SemanticProjectionOperation.Set, mapping.Source switch
                    {
                        SemanticResolvedExpression resolved => new SemanticProjectionEventProperty([resolved.Target]),
                        SemanticValueExpression literal => new SemanticProjectionLiteral(literal.Value),
                        _ => throw new UnsupportedSemanticMapping()
                    })).ToArray();
                    SemanticRunScopedMappings.Define(builder, types.For(transition.EventContract), mappings, model, @event, key, plan);
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
            output.Add(ProjectedInstance(model, source, instance, plan));
        }
        return output;
    }

    static Projected ProjectedInstance<TReadModel>(SemanticReadModel model, EventSourceId source, TReadModel instance, SemanticExecutionPlan plan)
    {
        // The sink stores the identifier as its document key, not as a field in the materialized model.
        var identifiers = model.Properties.Where(property => property.IsIdentifier).ToArray();
        var composite = identifiers.Length > 1 ? typeof(TReadModel).GetProperty("Id")!.GetValue(instance)! : null;
        var key = composite is null ? SemanticValue.Text(source.ToString()) : SemanticValue.Composite([.. identifiers.Select(property =>
            new SemanticPropertyValue(property.Id, Value(composite.GetType().GetProperty(property.Name)!.GetValue(composite), property.Type, plan)))]);
        var identity = composite is null
            ? new Dictionary<SemanticId, SemanticValue> { [identifiers[0].Id] = key }
            : ((SemanticCompositeValue)key).Properties.ToDictionary(part => part.TargetProperty, part => part.Value);
        var values = model.Properties.Select(property => new SemanticPropertyValue(
            property.Id,
            identity.TryGetValue(property.Id, out var value) ? value : Value(typeof(TReadModel).GetProperty(property.Name)!.GetValue(instance), property.Type, plan)))
            .ToImmutableArray();
        return new(model.Id, key, values);
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
            var transitionKey = projection.Transitions.SingleOrDefault(transition => transition.EventContract == fact.EventContract)?.AffectedInstance.Key;
            var fixtureKey = transitionKey is SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } resolvedKey
                ? fact.Values.Single(value => value.TargetProperty == resolvedKey.Target).Value
                : projectedKey switch
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
                var observing = 0;
                var orphaned = 0;
                foreach (var child in scope.Children)
                {
                    var parentKey = child.Scope.From.SingleOrDefault(from => from.EventContract == fact.EventContract)?.ParentKey ??
                        child.Scope.Removals.SingleOrDefault(removal => removal.EventContract == fact.EventContract)?.ParentKey;
                    if (!child.Scope.From.Any(from => from.EventContract == fact.EventContract) && !child.Scope.Removals.Any(removal => removal.EventContract == fact.EventContract)) continue;
                    observing++;
                    var parent = parentKey is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var property] } }
                        ? fact.Values.Single(value => value.TargetProperty == property).Value : destination;
                    if (parent is SemanticTextValue text && scenario.Instances.Keys.Any(source => source.ToString() == text.Value)) continue;

                    // The reference keeps a child with no parent pending for good, so it materializes nothing; Chronicle does the same.
                    if (ParentArrivesLater(scope, context, index, parent))
                        throw new SemanticProjectionLoweringRefused(new NotSupportedException("A child event before its parent instance is not admitted; Chronicle's testing scenario resolves deferred children in a different order from the reference (existing parent instance required)."));
                    orphaned++;
                }

                // One appended event feeds every child scope, so an orphan in only some of them cannot be dropped on its own.
                if (orphaned > 0 && orphaned < observing)
                    throw new SemanticProjectionLoweringRefused(new NotSupportedException("An event feeding several child collections with a missing parent in only some of them is not admitted (existing parent instance required in every collection)."));
                if (orphaned > 0)
                {
                    // Dropping the event would also drop whatever else the projection does with it.
                    if (HasOperationsBesidesDirectChildren(projection, scope, fact.EventContract))
                        throw new SemanticProjectionLoweringRefused(new NotSupportedException("An event with a missing child parent that the projection also uses for a root removal, a join, a transition or a nested child is not admitted (the orphan child cannot be dropped on its own)."));
                    continue;
                }
            }
            scenario.CollectEventsFor(new EventSourceId(key), [types.Materialize(fact)]);
        }
    }

    // Direct child from/remove is the only operation an orphan drop may cover; everything else on the event must stay.
    static bool HasOperationsBesidesDirectChildren(SemanticProjection projection, SemanticProjectionScope scope, SemanticId contract) =>
        projection.Transitions.Any(transition => transition.EventContract == contract) ||
        scope.Removals.Any(removal => removal.EventContract == contract) ||
        scope.Joins.Any(join => join.EventContract == contract) ||
        scope.Children.Any(child => child.Scope.Joins.Any(join => join.EventContract == contract) || child.Scope.Children.Any(grandchild => ObservesAnywhere(grandchild.Scope, contract)));

    static bool ObservesAnywhere(SemanticProjectionScope scope, SemanticId contract) =>
        scope.From.Any(from => from.EventContract == contract) ||
        scope.Removals.Any(removal => removal.EventContract == contract) ||
        scope.Joins.Any(join => join.EventContract == contract) ||
        scope.Children.Any(child => ObservesAnywhere(child.Scope, contract));

    // A later fact the root scope observes with the parent's key (or through a join) could still create the parent.
    static bool ParentArrivesLater(SemanticProjectionScope scope, SemanticRunContext context, int index, SemanticValue parent)
    {
        for (var later = index + 1; later < context.Facts.Count; later++)
        {
            var fact = context.Facts[later];
            if (scope.Joins.Any(join => join.EventContract == fact.EventContract)) return true;
            var from = scope.From.SingleOrDefault(candidate => candidate.EventContract == fact.EventContract);
            if (from is null) continue;
            var key = from.Key switch
            {
                SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var property] } } => fact.Values.Single(value => value.TargetProperty == property).Value,
                SemanticProjectionValueKey { Value: SemanticProjectionLiteral literal } => literal.Value,
                _ => context.Destinations[later]
            };
            if (SemanticExpectationComparer.AreEqual(key, parent)) return true;
        }
        return false;
    }

    static IEnumerable<SemanticSpecificationReadModel> Seeds(SemanticRunContext context, SemanticId model) =>
        context.WorldFixture.GivenReadModels.Where(seed => seed.ReadModel == model)
            .GroupBy(seed => SemanticRunContext.Canonical(seed.Key), StringComparer.Ordinal).Select(group => group.Last());

    static SemanticProjectionMapping[] FromMappings(SemanticProjectionScope scope, SemanticProjectionFrom from)
    {
        // Native every/all must preserve original from targets: Chronicle uses them to select join backfills.
        if (SemanticScopedProjectionSupport.RendersEveryLiteralsNatively(scope))
        {
            return [.. from.Mappings];
        }

        var literals = scope.Every?.Mappings.Where(mapping => mapping.Source is SemanticProjectionLiteral).ToArray() ?? [];
        return [.. from.Mappings.Where(mapping => mapping.Operation != SemanticProjectionOperation.Set || !literals.Any(literal => literal.Target.SequenceEqual(mapping.Target))).Concat(literals)];
    }

    static void DefineEvery<TReadModel>(IProjectionBuilderFor<TReadModel> builder, SemanticProjectionScope scope, SemanticProjectionEvery every, SemanticReadModel model)
    {
        if (every.SubscribesToAllEvents)
        {
            // FromAll has no child-inclusion switch; child scopes are refused before lowering.
            builder.FromAll(from => DefineEveryMappings(
                scope,
                model,
                (target, value) => SetEvery(from.Set(Property<TReadModel>(target)), target, value)));
        }
        else
        {
            builder.FromEvery(from =>
            {
                from.ExcludeChildProjections();
                DefineEveryMappings(
                    scope,
                    model,
                    (target, value) => SetEvery(from.Set(Property<TReadModel>(target)), target, value));
            });
        }
    }

    static void DefineEveryMappings(SemanticProjectionScope scope, SemanticReadModel model, Action<string, SemanticValue?> set)
    {
        var every = scope.Every!;
        var identifier = model.Properties.Single(property => property.IsIdentifier);
        if (every.SubscribesToAllEvents && !every.Mappings.Any(mapping => mapping.Target.Contains(identifier.Id)))
        {
            set(identifier.Name, null);
        }

        foreach (var mapping in SemanticScopedProjectionSupport.NativeEveryMappings(scope))
        {
            var target = model.Properties.Single(property => property.Id == mapping.Target[0]);
            set(target.Name, (mapping.Source as SemanticProjectionLiteral)?.Value);
        }
    }

    // A null value represents the event-source mapping, not a supported null literal Set.
    static void SetEvery<TReadModel, TParent>(IAllSetBuilder<TReadModel, TParent> set, string target, SemanticValue? value)
    {
        if (value is null)
        {
            set.ToEventSourceId();
        }
        else
        {
            SetLiteral(set, target, value);
        }
    }

    // IAllSetBuilder.ToValue infers its type from the argument, so it is called with the read model property type.
    static void SetLiteral<TReadModel, TParent>(IAllSetBuilder<TReadModel, TParent> set, string target, SemanticValue value)
    {
        var type = typeof(TReadModel).GetProperty(target)!.PropertyType;
        typeof(IAllSetBuilder<TReadModel, TParent>).GetMethod(nameof(IAllSetBuilder<,>.ToValue))!
            .MakeGenericMethod(type).Invoke(set, BindingFlags.DoNotWrapExceptions, null, [SemanticRuntimeTypes.ConvertValue(value, type)], null);
    }

    static Expression<Func<TReadModel, object>> Property<TReadModel>(string name)
    {
        var parameter = Expression.Parameter(typeof(TReadModel), "model");

        return Expression.Lambda<Func<TReadModel, object>>(Expression.Convert(Expression.Property(parameter, name), typeof(object)), parameter);
    }

    static void DefineRemoval<TReadModel>(IProjectionBuilderFor<TReadModel> builder, Type eventType, string? keyProperty, SemanticProjectionKey key, SemanticReadModel model, SemanticEventContract @event, SemanticExecutionPlan plan) =>
        typeof(SemanticRunProjections).GetMethod(nameof(RemoveEvent), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TReadModel), eventType).Invoke(null, BindingFlags.DoNotWrapExceptions, null, [builder, keyProperty, key, model, @event, plan], null);

    static void RemoveEvent<TReadModel, TEvent>(IProjectionBuilderFor<TReadModel> builder, string? keyProperty, SemanticProjectionKey key, SemanticReadModel model, SemanticEventContract @event, SemanticExecutionPlan plan)
    {
        // The generic parameter is a runtime-emitted event contract, as in DefineEvent.
#pragma warning disable CHR0002
        if (key is SemanticProjectionPartsKey parts)
        {
            builder.RemovedWith<TEvent>(removed => SemanticRunCompositeKeys.Define(removed, typeof(IKeyBuilder<TEvent, RemovedWithBuilder<TReadModel, TEvent>>), typeof(TReadModel), typeof(TEvent), model, @event, parts, plan));
        }
        else if (keyProperty is null)
        {
            builder.RemovedWith<TEvent>();
        }
        else
        {
            var parameter = Expression.Parameter(typeof(TEvent), "@event");
            var accessor = Expression.Lambda<Func<TEvent, object>>(Expression.Convert(Expression.Property(parameter, keyProperty), typeof(object)), parameter);
            builder.RemovedWith<TEvent>(removed => removed.UsingKey(accessor));
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
