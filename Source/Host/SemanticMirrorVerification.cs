// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Host;

internal static class SemanticMirrorVerification
{
    internal static void Check(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts, SemanticWorld world)
    {
        foreach (var projection in plan.Projections.Values)
        {
            var model = plan.ReadModels[projection.ReadModel];
            var expected = new Dictionary<SemanticValue, Dictionary<SemanticId, SemanticValue>>(new SemanticKeyComparer());
            foreach (var fact in facts)
            {
                foreach (var transition in projection.Transitions.Where(candidate => candidate.EventContract == fact.EventContract))
                {
                    var key = Resolve(transition.AffectedInstance.Key, fact);
                    if (!expected.TryGetValue(key, out var state))
                    {
                        state = [];
                        expected.Add(key, state);
                    }

                    foreach (var mapping in transition.Mappings)
                    {
                        state[mapping.TargetProperty] = Resolve(mapping.Source, fact);
                    }
                }
            }

            var actual = world.ReadModels.Where(instance => instance.ReadModel == model.Id).ToArray();
            if (expected.Count != actual.Length || expected.Any(pair =>
                !actual.Any(instance => new SemanticKeyComparer().Equals(instance.Key, pair.Key) &&
                    instance.Values.Length == model.Properties.Length &&
                    instance.Values.All(value => pair.Value.TryGetValue(value.TargetProperty, out var expectedValue) &&
                        new SemanticKeyComparer().Equals(value.Value, expectedValue)))))
            {
                throw new SemanticWorldRebuildRefused($"Chronicle mirror for '{model.Name}' disagrees with the reference projection of the event log.");
            }
        }
    }

    static SemanticValue Resolve(SemanticExpression expression, SemanticFact fact) => expression switch
    {
        SemanticResolvedExpression { Root: SemanticExpressionRootKind.Event, Source: SemanticExpressionSourceKind.Property } resolved =>
            fact.Values.Single(value => value.TargetProperty == resolved.Target).Value,
        SemanticValueExpression literal => literal.Value,
        _ => throw new SemanticWorldRebuildRefused("A projection expression cannot be verified during world reconstruction.")
    };

    sealed class SemanticKeyComparer : IEqualityComparer<SemanticValue>
    {
        public bool Equals(SemanticValue? x, SemanticValue? y) => (x, y) switch
        {
            (SemanticNullValue, SemanticNullValue) => true,
            (SemanticTextValue a, SemanticTextValue b) => a.Value == b.Value,
            (SemanticNumberValue a, SemanticNumberValue b) => a.Value == b.Value,
            (SemanticBooleanValue a, SemanticBooleanValue b) => a.Value == b.Value,
            (SemanticArrayValue a, SemanticArrayValue b) => a.Values.Length == b.Values.Length && a.Values.Zip(b.Values).All(pair => Equals(pair.First, pair.Second)),
            (SemanticCompositeValue a, SemanticCompositeValue b) => a.Properties.Length == b.Properties.Length && a.Properties.All(value => b.Properties.Any(other => other.TargetProperty == value.TargetProperty && Equals(other.Value, value.Value))),
            _ => false
        };

        public int GetHashCode(SemanticValue obj) => obj switch
        {
            SemanticTextValue text => text.Value.GetHashCode(StringComparison.Ordinal),
            SemanticNumberValue number => number.Value.GetHashCode(),
            SemanticBooleanValue boolean => boolean.Value.GetHashCode(),
            _ => throw new SemanticWorldRebuildRefused("A mirrored projection uses an unsupported key shape.")
        };
    }
}
