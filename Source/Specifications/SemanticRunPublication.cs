// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications;

// Publications are derived facts, never reference-evaluator settlement results.
internal sealed class SemanticRunPublication(SemanticExecutionPlan plan, SemanticRunContext context, int historyCount)
{
    readonly int _historyCount = historyCount;
    int _next = historyCount;

    internal async Task<SemanticRunReactionFailure?> PublishPending(CancellationToken cancellationToken)
    {
        var start = _next;
        var end = context.Facts.Count;
        _next = end;
        foreach (var projection in plan.Projections.Values.Where(projection => SemanticTranslationSupport.Projection(projection, plan.Events) && plan.Runs(projection.Id)).OrderBy(projection => projection.Id.ToString(), StringComparer.Ordinal))
        {
            var target = plan.Events[projection.ReadModel];
            var states = new Dictionary<SemanticValue, Dictionary<SemanticId, SemanticValue>>();
            var last = new Dictionary<SemanticValue, IReadOnlyList<SemanticPropertyValue>>();
            for (var index = 0; index < context.Facts.Count; index++)
            {
                if ((index < start || index >= end) && context.Facts[index].EventContract == target.Id) last[context.Destinations[index]] = context.Facts[index].Values;
            }
            for (var index = 0; index < end; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fact = context.Facts[index];
                var from = projection.Scope!.From.Where(candidate => candidate.EventContract == fact.EventContract).ToArray();
                if (from.Length == 0) continue;
                var key = context.Destinations[index];
                if (!states.TryGetValue(key, out var state)) states[key] = state = [];
                foreach (var mapping in from.SelectMany(candidate => candidate.Mappings)) state[mapping.Target[0]] = Evaluate(mapping.Source!, fact, key);
                if (index < start) continue;
                var values = new List<SemanticPropertyValue>();
                var complete = true;
                foreach (var property in target.Properties)
                {
                    if (!state.TryGetValue(property.Id, out var value) || (value is SemanticNullValue && !property.Type.IsOptional))
                    {
                        if (!property.Type.IsOptional)
                        {
                            complete = false;
                            break;
                        }
                        value = SemanticValue.Null;
                    }
                    try
                    {
                        SemanticRunReactionExpressions.Validate(plan, value, property.Type, $"event property '{property.Name}'");
                    }
                    catch (InvalidSemanticContract exception)
                    {
                        return SemanticRunReactionFailure.Unsupported($"Projection '{projection.Name}' cannot publish '{target.Name}': {exception.Message}");
                    }
                    values.Add(new(property.Id, value));
                }
                if (!complete || (last.TryGetValue(key, out var previous) && SameValues(previous, values))) continue;
                if (context.Facts.Count - _historyCount >= SemanticRunReactions.MaximumFacts) return SemanticRunReactions.FactLimit();
                await context.Append(new(target.Id, [.. values]) { EventSource = fact.EventSource, Route = fact.Route }, key, context.Occurrences[index], cancellationToken);
                last[key] = values;
            }
        }

        // Derived publications have already been folded; do not publish them again as a new batch.
        _next = context.Facts.Count;
        return null;
    }

    static bool SameValues(IReadOnlyList<SemanticPropertyValue> previous, List<SemanticPropertyValue> current) =>
        previous.Count == current.Count && previous.All(property => current.Exists(candidate => candidate.TargetProperty == property.TargetProperty && SemanticExpectationComparer.AreEqual(candidate.Value, property.Value)));

    static SemanticValue Evaluate(SemanticProjectionValue source, SemanticSpecificationEvent fact, SemanticValue destination) => source switch
    {
        SemanticProjectionLiteral literal => literal.Value,
        SemanticProjectionEventSourceIdentity => destination,
        SemanticProjectionEventContextValue context when context.Path == "eventSourceId" => destination,
        SemanticProjectionEventProperty property => Path(fact.Values, property.Path),
        _ => throw new InvalidSemanticContract("A publishing projection reads an unsupported value source.")
    };

    static SemanticValue Path(IEnumerable<SemanticPropertyValue> values, IEnumerable<SemanticId> path)
    {
        var value = SemanticValue.Null;
        foreach (var id in path)
        {
            value = values.SingleOrDefault(property => property.TargetProperty == id)?.Value ?? SemanticValue.Null;
            values = value is SemanticCompositeValue composite ? composite.Properties : [];
        }
        return value;
    }
}
