// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Screenplay.Semantics;
using Cratis.Serialization;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.Comparison;

internal static partial class SemanticRunProjections
{
    static void SeedReadModels<TReadModel>(ReadModelScenario<TReadModel> scenario, SemanticReadModel model, SemanticRunContext context, SemanticRuntimeTypes types)
        where TReadModel : class
    {
        foreach (var seed in Seeds(context, model.Id))
        {
            var instance = SeedInstance<TReadModel>(model, seed, types);
            if (seed.Key is SemanticCompositeValue composite)
            {
                scenario.CollectReadModelFor(CompositeSeedParts(instance, model, composite, types), instance);
            }
            else
            {
                scenario.CollectReadModelFor(new EventSourceId(((SemanticTextValue)seed.Key).Value), instance);
            }
        }
    }

    static TReadModel SeedInstance<TReadModel>(SemanticReadModel model, SemanticSpecificationReadModel seed, SemanticRuntimeTypes types)
    {
        var instance = Activator.CreateInstance<TReadModel>();
        foreach (var value in seed.Values)
        {
            var property = model.Properties.Single(property => property.Id == value.TargetProperty);
            typeof(TReadModel).GetProperty(property.Name)!.SetValue(instance, types.ReadModelValue(value.Value, property.Type));
        }
        return instance;
    }

    static Dictionary<string, object?> CompositeSeedParts<TReadModel>(TReadModel instance, SemanticReadModel model, SemanticCompositeValue composite, SemanticRuntimeTypes types)
    {
        var key = Activator.CreateInstance(types.ForReadModelKey(model))!;
        foreach (var part in composite.Properties)
        {
            var property = model.Properties.Single(property => property.Id == part.TargetProperty);
            key.GetType().GetProperty(property.Name)!.SetValue(key, types.ReadModelValue(part.Value, property.Type));
        }
        typeof(TReadModel).GetProperty("Id")!.SetValue(instance, key);
        var naming = new CamelCaseNamingPolicy();
        return model.Properties.Where(property => property.IsIdentifier).ToDictionary(
            property => naming.GetPropertyName(property.Name),
            property => typeof(TReadModel).GetProperty(property.Name)!.GetValue(instance),
            StringComparer.Ordinal);
    }

    static SemanticValue FixtureKey(SemanticProjectionKey? key, SemanticExpression? transition, SemanticSpecificationEvent fact, SemanticValue destination)
    {
        if (key is SemanticProjectionPartsKey parts)
        {
            return SemanticValue.Composite([.. parts.Parts.Select(part => new SemanticPropertyValue(part.Property, FixturePart(fact.Values, ((SemanticProjectionEventProperty)part.Value).Path)))]);
        }
        if (transition is SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } resolved)
        {
            return fact.Values.Single(value => value.TargetProperty == resolved.Target).Value;
        }
        return key switch
        {
            SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var property] } } => fact.Values.Single(value => value.TargetProperty == property).Value,
            SemanticProjectionValueKey { Value: SemanticProjectionLiteral literal } => literal.Value,
            _ => destination
        };
    }

    static SemanticValue FixturePart(IEnumerable<SemanticPropertyValue> values, IEnumerable<SemanticId> path)
    {
        var value = SemanticValue.Null;
        foreach (var id in path)
        {
            value = values.Single(part => part.TargetProperty == id).Value;
            values = value is SemanticCompositeValue composite ? composite.Properties : [];
        }
        return value;
    }
}
