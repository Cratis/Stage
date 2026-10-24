// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.Comparison;

internal static class SemanticRunScopedMappings
{
    internal static void Define<TReadModel>(IProjectionBuilderFor<TReadModel> builder, Type eventType, SemanticProjectionMapping[] mappings, SemanticReadModel model, SemanticEventContract @event, SemanticProjectionKey key, SemanticExecutionPlan plan) =>
        typeof(SemanticRunScopedMappings).GetMethod(nameof(DefineEvent), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TReadModel), eventType).Invoke(null, BindingFlags.DoNotWrapExceptions, null, [builder, mappings, model, @event, key, plan], null);

    internal static void DefineRelated<TReadModel, TBuilder>(IProjectionBuilder<TReadModel, TBuilder> builder, SemanticProjectionScope scope, ImmutableArray<SemanticProperty> properties, SemanticExecutionPlan plan, SemanticRuntimeTypes types)
        where TBuilder : class
    {
        foreach (var join in scope.Joins)
        {
            typeof(SemanticRunScopedMappings).GetMethod(nameof(DefineJoin), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(TReadModel), types.For(join.EventContract), typeof(TBuilder))
                .Invoke(null, BindingFlags.DoNotWrapExceptions, null, [builder, join, properties, plan, typeof(TBuilder) != typeof(IProjectionBuilderFor<TReadModel>)], null);
        }
        foreach (var child in scope.Children)
        {
            var property = properties.Single(property => property.Id == child.Property);
            typeof(SemanticRunScopedMappings).GetMethod(nameof(DefineChildren), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(TReadModel), types.ResolveReadModel(property.Type with { IsCollection = false }), typeof(TBuilder))
                .Invoke(null, BindingFlags.DoNotWrapExceptions, null, [builder, child, property.Name, plan, types], null);
        }
    }

    static void DefineEvent<TReadModel, TEvent>(IProjectionBuilderFor<TReadModel> builder, SemanticProjectionMapping[] mappings, SemanticReadModel model, SemanticEventContract @event, SemanticProjectionKey key, SemanticExecutionPlan plan)
    {
        // TEvent is a runtime-emitted contract carrying [EventType].
#pragma warning disable CHR0002
        builder.From<TEvent>(from =>
        {
            if (key is SemanticProjectionValueKey { Value: SemanticProjectionLiteral { Value: SemanticTextValue text } })
            {
                from.UsingConstantKey(text.Value);
            }
            else if (key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var id] } })
            {
                from.UsingKey(Property<TEvent, object>(@event.Properties.Single(property => property.Id == id).Name));
            }
            foreach (var mapping in mappings)
            {
                var target = model.Properties.Single(property => property.Id == mapping.Target[0]);
                var source = MappingSource(plan, @event, model.Properties, mapping);
                typeof(SemanticRunScopedMappings).GetMethod(nameof(Apply), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(typeof(TReadModel), typeof(TEvent), typeof(TReadModel).GetProperty(target.Name)!.PropertyType, typeof(IFromBuilder<TReadModel, TEvent>))
                    .Invoke(null, BindingFlags.DoNotWrapExceptions, null, [from, mapping, target.Name, source], null);
            }
        });
#pragma warning restore CHR0002
    }

    static void Apply<TReadModel, TEvent, TValue, TBuilder>(IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder> from, SemanticProjectionMapping mapping, string target, string? source)
        where TBuilder : class, IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder>
    {
        var property = Property<TReadModel, TValue>(target);
        try
        {
            switch (mapping.Operation)
            {
                case SemanticProjectionOperation.Set:
                    if (mapping.Source is SemanticProjectionLiteral literal)
                    {
                        from.Set(property).ToValue((TValue)SemanticRuntimeTypes.ConvertValue(literal.Value, typeof(TValue))!);
                    }
                    else if (source is not null)
                    {
                        from.Set(new PropertyPath(target)).To(new PropertyPath(source));
                    }
                    else if (mapping.Source is SemanticProjectionEventSourceIdentity)
                    {
                        from.Set(new PropertyPath(target)).ToEventSourceId();
                    }
                    else
                    {
                        throw Refused(mapping);
                    }
                    break;
                case SemanticProjectionOperation.Add:
                case SemanticProjectionOperation.Subtract:
                    if (mapping.Source is SemanticProjectionLiteral { Value: SemanticNumberValue { Value: 0m } }) break;
                    if (source is null) throw Refused(mapping);
                    if (mapping.Operation == SemanticProjectionOperation.Add) from.Add(property).With(Property<TEvent, TValue>(source));
                    else from.Subtract(property).With(Property<TEvent, TValue>(source));
                    break;
                case SemanticProjectionOperation.Increment:
                    from.Increment(property);
                    break;
                case SemanticProjectionOperation.Decrement:
                    from.Decrement(property);
                    break;
                default:
                    throw Refused(mapping);
            }
        }
        catch (UnsupportedSemanticMapping exception)
        {
            throw Refused(mapping, exception);
        }
    }

    static void DefineChildren<TParent, TChild, TBuilder>(IProjectionBuilder<TParent, TBuilder> builder, SemanticProjectionChildren child, string name, SemanticExecutionPlan plan, SemanticRuntimeTypes types)
        where TBuilder : class
    {
        var property = plan.Model.Application.Types.SelectMany(type => type.Properties).Concat(plan.ReadModels.Values.SelectMany(model => model.Properties)).Single(property => property.Id == child.Property);
        var properties = plan.Model.Application.Types.Single(type => type.Id == property.Type.Target).Properties;
        builder.Children(Property<TParent, IEnumerable<TChild>>(name), children =>
        {
            children.NoAutoMap();
            children.IdentifiedBy(new PropertyPath(properties.Single(property => property.Id == child.IdentifiedBy).Name));
            foreach (var from in child.Scope.From)
            {
                typeof(SemanticRunScopedMappings).GetMethod(nameof(DefineChildEvent), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(typeof(TParent), typeof(TChild), types.For(from.EventContract))
                    .Invoke(null, BindingFlags.DoNotWrapExceptions, null, [children, from, properties, plan], null);
            }
            foreach (var removal in child.Scope.Removals)
            {
                typeof(SemanticRunScopedMappings).GetMethod(nameof(DefineChildRemoval), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(typeof(TParent), typeof(TChild), types.For(removal.EventContract))
                    .Invoke(null, BindingFlags.DoNotWrapExceptions, null, [children, removal, plan], null);
            }
            DefineRelated(children, child.Scope, properties, plan, types);
        });
    }

    static void DefineChildEvent<TParent, TChild, TEvent>(IChildrenBuilder<TParent, TChild> children, SemanticProjectionFrom definition, ImmutableArray<SemanticProperty> properties, SemanticExecutionPlan plan)
    {
#pragma warning disable CHR0002
        children.From<TEvent>(from =>
        {
            var @event = plan.Events[definition.EventContract];
            if (definition.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var key] } })
                from.UsingKey(Property<TEvent, object>(@event.Properties.Single(property => property.Id == key).Name));
            if (definition.ParentKey is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var parent] } })
                from.UsingParentKey(Property<TEvent, object>(@event.Properties.Single(property => property.Id == parent).Name));
            ApplyMappings(from, definition.Mappings, properties, @event, plan);
        });
#pragma warning restore CHR0002
    }

    static void DefineChildRemoval<TParent, TChild, TEvent>(IChildrenBuilder<TParent, TChild> children, SemanticProjectionRemoval definition, SemanticExecutionPlan plan)
    {
#pragma warning disable CHR0002
        children.RemovedWith<TEvent>(removed =>
        {
            var @event = plan.Events[definition.EventContract];
            if (definition.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var key] } })
                removed.UsingKey(Property<TEvent, object>(@event.Properties.Single(property => property.Id == key).Name));
            if (definition.ParentKey is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var parent] } })
                removed.UsingParentKey(Property<TEvent, object>(@event.Properties.Single(property => property.Id == parent).Name));
        });
#pragma warning restore CHR0002
    }

    static void DefineJoin<TReadModel, TEvent, TBuilder>(IProjectionBuilder<TReadModel, TBuilder> builder, SemanticProjectionJoin definition, ImmutableArray<SemanticProperty> properties, SemanticExecutionPlan plan, bool child)
        where TBuilder : class
    {
#pragma warning disable CHR0002
        builder.Join<TEvent>(join =>
        {
            if (!child) join.On(Property<TReadModel, object>(properties.Single(property => property.Id == definition.On).Name));
            ApplyMappings(join, definition.Mappings, properties, plan.Events[definition.EventContract], plan);
        });
#pragma warning restore CHR0002
    }

    static void ApplyMappings<TReadModel, TEvent, TBuilder>(IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder> builder, ImmutableArray<SemanticProjectionMapping> mappings, ImmutableArray<SemanticProperty> properties, SemanticEventContract @event, SemanticExecutionPlan plan)
        where TBuilder : class, IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder>
    {
        foreach (var mapping in mappings)
        {
            var target = properties.Single(property => property.Id == mapping.Target[0]);
            var source = mapping.Source is SemanticProjectionEventProperty property ? SourceName(plan, @event, property.Path) : null;
            typeof(SemanticRunScopedMappings).GetMethod(nameof(Apply), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(TReadModel), typeof(TEvent), typeof(TReadModel).GetProperty(target.Name)!.PropertyType, typeof(TBuilder))
                .Invoke(null, BindingFlags.DoNotWrapExceptions, null, [builder, mapping, target.Name, source], null);
        }
    }

    static string? MappingSource(SemanticExecutionPlan plan, SemanticEventContract @event, ImmutableArray<SemanticProperty> targets, SemanticProjectionMapping mapping)
    {
        if (mapping.Factor is not null) return ProductSource(plan, @event, targets, mapping);
        return mapping.Source is SemanticProjectionEventProperty property ? SourceName(plan, @event, property.Path) : null;
    }

    // The member a product is carried in: the event holds the multiplied value, which Chronicle's Add and Subtract then read as one property.
    static string ProductSource(SemanticExecutionPlan plan, SemanticEventContract @event, ImmutableArray<SemanticProperty> targets, SemanticProjectionMapping mapping) =>
        SemanticProjectionProducts.Rejection(mapping, true, @event, targets, new SemanticApplicationContext(plan), out var product) is null
            ? SemanticRuntimeTypes.ProductMember(product!)
            : throw Refused(mapping);

    // An event property, or a member reached through composite values: the dotted path Chronicle reads from the event.
    static string SourceName(SemanticExecutionPlan plan, SemanticEventContract @event, ImmutableArray<SemanticId> path)
    {
        var names = new List<string> { @event.Properties.Single(value => value.Id == path[0]).Name };
        var members = plan.Model.Application.Types.SelectMany(type => type.Properties).ToDictionary(member => member.Id, member => member.Name);
        names.AddRange(path.Skip(1).Select(id => members[id]));
        return string.Join('.', names);
    }

    static SemanticProjectionLoweringRefused Refused(SemanticProjectionMapping mapping, Exception? cause = null) =>
        new(new NotSupportedException($"Scoped {mapping.Operation} mapping with source kind '{mapping.Source?.GetType().Name ?? "none"}' is not admitted.", cause));

    static Expression<Func<T, TValue>> Property<T, TValue>(string name)
    {
        var parameter = Expression.Parameter(typeof(T), "value");
        return Expression.Lambda<Func<T, TValue>>(Expression.Convert(Expression.Property(parameter, name), typeof(TValue)), parameter);
    }
}
