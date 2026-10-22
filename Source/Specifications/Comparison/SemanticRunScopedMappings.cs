// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.Comparison;

internal static class SemanticRunScopedMappings
{
    internal static void Define<TReadModel>(IProjectionBuilderFor<TReadModel> builder, Type eventType, SemanticProjectionMapping[] mappings, SemanticReadModel model, SemanticEventContract @event) =>
        typeof(SemanticRunScopedMappings).GetMethod(nameof(DefineEvent), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TReadModel), eventType).Invoke(null, BindingFlags.DoNotWrapExceptions, null, [builder, mappings, model, @event], null);

    static void DefineEvent<TReadModel, TEvent>(IProjectionBuilderFor<TReadModel> builder, SemanticProjectionMapping[] mappings, SemanticReadModel model, SemanticEventContract @event)
    {
        // TEvent is a runtime-emitted contract carrying [EventType].
#pragma warning disable CHR0002
        builder.From<TEvent>(from =>
        {
            foreach (var mapping in mappings)
            {
                var target = model.Properties.Single(property => property.Id == mapping.Target[0]);
                var source = mapping.Source is SemanticProjectionEventProperty property ? @event.Properties.Single(value => value.Id == property.Path[0]).Name : null;
                typeof(SemanticRunScopedMappings).GetMethod(nameof(Apply), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(typeof(TReadModel), typeof(TEvent), typeof(TReadModel).GetProperty(target.Name)!.PropertyType)
                    .Invoke(null, BindingFlags.DoNotWrapExceptions, null, [from, mapping, target.Name, source], null);
            }
        });
#pragma warning restore CHR0002
    }

    static void Apply<TReadModel, TEvent, TValue>(IFromBuilder<TReadModel, TEvent> from, SemanticProjectionMapping mapping, string target, string? source)
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

    static SemanticProjectionLoweringRefused Refused(SemanticProjectionMapping mapping, Exception? cause = null) =>
        new(new NotSupportedException($"Scoped {mapping.Operation} mapping with source kind '{mapping.Source?.GetType().Name ?? "none"}' is not admitted.", cause));

    static Expression<Func<T, TValue>> Property<T, TValue>(string name)
    {
        var parameter = Expression.Parameter(typeof(T), "value");
        return Expression.Lambda<Func<T, TValue>>(Expression.Convert(Expression.Property(parameter, name), typeof(TValue)), parameter);
    }
}
