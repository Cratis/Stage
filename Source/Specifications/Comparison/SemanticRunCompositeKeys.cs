// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reflection;
using Cratis.Chronicle.Projections;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Comparison;

internal static class SemanticRunCompositeKeys
{
    internal static void Define(object builder, Type contract, Type readModelType, Type eventType, SemanticReadModel model, SemanticEventContract @event, SemanticProjectionPartsKey parts, SemanticExecutionPlan plan)
    {
        var keyType = readModelType.GetProperty("Id")!.PropertyType;
        var callback = typeof(SemanticRunCompositeKeys).GetMethod(nameof(Callback), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(keyType, eventType).Invoke(null, [model, @event, parts, plan]);
        contract.GetMethod(nameof(IKeyBuilder<,>.UsingCompositeKey))!
            .MakeGenericMethod(keyType).Invoke(builder, BindingFlags.DoNotWrapExceptions, null, [callback], null);
    }

    static Action<ICompositeKeyBuilder<TKey, TEvent>> Callback<TKey, TEvent>(SemanticReadModel model, SemanticEventContract @event, SemanticProjectionPartsKey parts, SemanticExecutionPlan plan) => key =>
    {
        foreach (var property in model.Properties.Where(property => property.IsIdentifier).OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            var source = (SemanticProjectionEventProperty)parts.Parts.Single(part => part.Property == property.Id).Value;
            typeof(SemanticRunCompositeKeys).GetMethod(nameof(Set), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(TKey), typeof(TEvent), typeof(TKey).GetProperty(property.Name)!.PropertyType)
                .Invoke(null, BindingFlags.DoNotWrapExceptions, null, [key, property.Name, SourcePath(@event, source, plan)], null);
        }
    };

    static void Set<TKey, TEvent, TValue>(ICompositeKeyBuilder<TKey, TEvent> key, string target, string[] source)
    {
        var part = Expression.Parameter(typeof(TKey), "part");
        var evt = Expression.Parameter(typeof(TEvent), "evt");
        var value = source.Aggregate((Expression)evt, Expression.Property);
        if (value.Type != typeof(TValue)) value = Expression.Convert(value, typeof(TValue));
        key.Set(Expression.Lambda<Func<TKey, TValue>>(Expression.Property(part, target), part))
            .To(Expression.Lambda<Func<TEvent, TValue>>(value, evt));
    }

    static string[] SourcePath(SemanticEventContract @event, SemanticProjectionEventProperty source, SemanticExecutionPlan plan)
    {
        var properties = @event.Properties;
        var names = new List<string>();
        foreach (var id in source.Path)
        {
            var property = properties.Single(property => property.Id == id);
            names.Add(property.Name);
            if (property.Type.Kind == SemanticTypeReferenceKind.CompositeType)
            {
                properties = plan.Model.Application.Types.Single(type => type.Id == property.Type.Target).Properties;
            }
        }

        return [.. names];
    }
}
