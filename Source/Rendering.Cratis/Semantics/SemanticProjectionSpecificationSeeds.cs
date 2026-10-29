// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Preserves per-key fixtures before replaying the action's projection facts.
/// </summary>
internal static class SemanticProjectionSpecificationSeeds
{
    internal static IEnumerable<SemanticSpecificationReadModel> For(SemanticSpecification specification, SemanticId readModel) =>
        specification.GivenReadModels.Where(seed => seed.ReadModel == readModel).GroupBy(seed => seed.Key).Select(group => group.Last());

    internal static bool Contains(SemanticSpecification specification, SemanticSpecificationReadModel expected) =>
        For(specification, expected.ReadModel).Any(seed => Equals(seed.Key, expected.Key));

    internal static IReadOnlyList<SemanticSpecificationEvent> Given(SemanticSpecification specification, SemanticProjection projection)
    {
        var seeds = For(specification, projection.ReadModel).ToArray();
        var history = specification.GivenEvents.Where(fact => !seeds.Any(seed => Identifies(projection, fact, seed.Key)));

        // TryLower leaves a seeded append separate: unlike history, it must fold over the fixture.
        return specification.WhenAppended is { } appended
            ? [.. history, new(appended.EventContract, appended.Values) { EventSource = appended.EventSource, Route = appended.Route }]
            : [.. history];
    }

    internal static void Render(
        CSharpCodeBuilder builder,
        SemanticSpecification specification,
        SemanticReadModel readModel,
        SemanticTypeSystem types)
    {
        var identifiers = readModel.Properties.Where(property => property.IsIdentifier).OrderBy(property => property.Id.ToString(), StringComparer.Ordinal).ToArray();
        var name = types.SliceType(readModel.Id, readModel.Name);
        foreach (var seed in For(specification, readModel.Id))
        {
            var values = readModel.Properties.Select(property => $"{Identifiers.ToPascalCase(property.Name)}: {types.Value(seed.Values.Single(value => value.TargetProperty == property.Id).Value, property.Type)}");
            if (seed.Key is SemanticCompositeValue composite)
            {
                var parts = identifiers.Select(property => types.Value(composite.Properties.Single(part => part.TargetProperty == property.Id).Value, property.Type));
                var keyType = types.SliceType(readModel.Id, Projections.SemanticCompositeKeyAdmission.KeyTypeName(readModel));
                var entries = identifiers.Select(property => $"[new global::Cratis.Serialization.CamelCaseNamingPolicy().GetPropertyName(nameof({name}.{Identifiers.ToPascalCase(property.Name)}))] = {types.Value(composite.Properties.Single(part => part.TargetProperty == property.Id).Value, property.Type)}");
                builder.Line($"await _scenario.Given.ForKey(new global::System.Collections.Generic.Dictionary<string, object?> {{ {string.Join(", ", entries)} }})" +
                    $".ReadModel(new {name}(Id: new {keyType}({string.Join(", ", parts)}), {string.Join(", ", values)}));");
            }
            else
            {
                var key = types.EventSourceExpression(types.Value(seed.Key, identifiers[0].Type), identifiers[0].Type);
                builder.Line($"await _scenario.Given.ForEventSource({key}).ReadModel(new {name}({string.Join(", ", values)}));");
            }
        }
    }

    internal static bool KeyIdentifies(SemanticProjectionKey candidate, SemanticSpecificationEvent fact, SemanticValue key) => candidate switch
    {
        SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } => Equals(fact.EventSource?.Value, key),
        SemanticProjectionValueKey { Value: SemanticProjectionEventProperty { Path: [var property] } } =>
            Equals(fact.Values.SingleOrDefault(value => value.TargetProperty == property)?.Value, key),
        SemanticProjectionValueKey { Value: SemanticProjectionLiteral literal } => Equals(literal.Value, key),
        SemanticProjectionPartsKey parts when key is SemanticCompositeValue composite =>
            parts.Parts.Length == composite.Properties.Length && parts.Parts.All(part =>
                composite.Properties.SingleOrDefault(property => property.TargetProperty == part.Property) is { } expected &&
                KeyIdentifies(new SemanticProjectionValueKey(part.Value), fact, expected.Value)),
        _ => false
    };

    static bool Identifies(SemanticProjection projection, SemanticSpecificationEvent fact, SemanticValue key)
    {
        if (projection.Scope is not { } scope)
        {
            return projection.Transitions.Any(transition => transition.EventContract == fact.EventContract) && Equals(fact.EventSource?.Value, key);
        }
        var keys = scope.From.Where(from => from.EventContract == fact.EventContract).Select(from => from.Key)
            .Concat(scope.Removals.Where(removal => removal.EventContract == fact.EventContract).Select(removal => removal.Key));
        if (keys.Any(candidate => KeyIdentifies(candidate, fact, key))) return true;
        return scope.Every is { SubscribesToAllEvents: true } && !keys.Any() && Equals(fact.EventSource?.Value, key);
    }
}
