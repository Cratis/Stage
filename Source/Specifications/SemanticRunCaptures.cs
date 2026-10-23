// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications;

// Stage-owned events-source record capture settlement, ported from Screenplay v4.240.2.
internal sealed partial class SemanticRunCaptures(SemanticExecutionPlan plan, SemanticRunContext context)
{
    readonly Dictionary<(SemanticId Capture, SemanticValue Key), SemanticCaptureRecord> _records = [];
    int _historyCount;

    internal SemanticRunReactionFailure? Seed(int count)
    {
        _historyCount = count;
        for (var index = 0; index < count; index++)
        {
            foreach (var capture in Matching(context.Facts[index]))
            {
                var record = EventRecord(context.Facts[index]);
                var key = EventKey(capture, context.Facts[index], record);
                if (key is not (SemanticTextValue or SemanticNumberValue)) return UnsupportedKey(capture);
                _records[(capture.Id, key)] = Overlay(_records.GetValueOrDefault((capture.Id, key)), record);
            }
        }
        return null;
    }

    internal async Task<SemanticRunReactionFailure?> Deliver(int index, Func<CancellationToken, Task<SemanticRunReactionFailure?>> settle, CancellationToken cancellationToken)
    {
        var fact = context.Facts[index];
        foreach (var capture in Matching(fact))
        {
            var incoming = EventRecord(fact);
            var key = EventKey(capture, fact, incoming);
            if (key is not (SemanticTextValue or SemanticNumberValue)) return UnsupportedKey(capture);
            var previous = _records.GetValueOrDefault((capture.Id, key));
            var current = Overlay(previous, incoming);
            if (previous is not null && SameState(previous, current)) continue;

            // Observe before settling resulting facts, including reentrant deliveries.
            _records[(capture.Id, key)] = current;
            var evaluation = new Evaluation(plan, capture, key, previous, current);
            evaluation.ValidateSources(capture.Appends);
            foreach (var children in capture.Children) evaluation.ValidateSources(children.Appends);
            if (await Emit(evaluation, capture.Appends, key, index, settle, cancellationToken) is { } rootFailure) return rootFailure;

            // Each child collection follows the record's own appends: current children in order, then the removed ones in their earlier order.
            foreach (var children in capture.Children)
            {
                var was = Records(previous, children.Field);
                var now = Records(current, children.Field);
                var wasById = Index(was, children.IdentifiedBy);
                var nowById = Index(now, children.IdentifiedBy);
                foreach (var child in now)
                {
                    var childEvaluation = new Evaluation(plan, capture, key, wasById.GetValueOrDefault(Identity(child, children.IdentifiedBy)), child, current);
                    if (await Emit(childEvaluation, children.Appends, key, index, settle, cancellationToken) is { } childFailure) return childFailure;
                }
                foreach (var removed in was.Where(record => !nowById.ContainsKey(Identity(record, children.IdentifiedBy))))
                {
                    var childEvaluation = new Evaluation(plan, capture, key, removed, null, current);
                    if (await Emit(childEvaluation, children.Appends, key, index, settle, cancellationToken) is { } childFailure) return childFailure;
                }
            }
        }
        return null;
    }

    async Task<SemanticRunReactionFailure?> Emit(
        Evaluation evaluation,
        System.Collections.Immutable.ImmutableArray<SemanticCaptureAppend> appends,
        SemanticValue key,
        int index,
        Func<CancellationToken, Task<SemanticRunReactionFailure?>> settle,
        CancellationToken cancellationToken)
    {
        foreach (var append in appends)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var holds = evaluation.Holds(append.When);
            if (evaluation.Unsupported is { } unsupported) return SemanticRunReactionFailure.Unsupported(unsupported);
            if (!holds) continue;
            var produced = evaluation.Produce(append);
            if (evaluation.Unsupported is { } missing) return SemanticRunReactionFailure.Unsupported(missing);

            // A terminal disposition from an earlier append must precede later guards and mappings.
            var history = context.Facts.Select((value, offset) => new SemanticConstraintEvaluator.Fact(value.EventContract, context.Destinations[offset], value.Values)).ToArray();
            if (SemanticConstraintEvaluator.FindViolation(plan, history, [new(produced.EventContract, key, produced.Values)]) is { } violation)
            {
                return SemanticRunReactionFailure.Reject(SemanticConstraintEvaluator.Message(violation), violation.Name);
            }
            if (SemanticRunAdmission.CheckProjectionFact(plan, produced) is { } blocker) return SemanticRunReactionFailure.Refused(blocker);
            if (context.Facts.Count - _historyCount >= SemanticRunReactions.MaximumFacts) return SemanticRunReactions.FactLimit();
            await context.Append(produced, key, context.Occurrences[index], cancellationToken);
            if (await settle(cancellationToken) is { } failure) return failure;
        }
        return null;
    }

    static System.Collections.Immutable.ImmutableArray<SemanticCaptureRecord> Records(SemanticCaptureRecord? record, string field) =>
        record?.Fields.SingleOrDefault(value => value.Name == field) switch
        {
            null => [],
            { Kind: SemanticCaptureFieldKind.Records } value => value.Records,
            _ => throw new InvalidSemanticContract($"Capture children '{field}' must be a collection of records.")
        };

    static SemanticValue Identity(SemanticCaptureRecord record, string field) => record.Fields.SingleOrDefault(value => value.Name == field) switch
    {
        { Kind: SemanticCaptureFieldKind.Value, Value: SemanticTextValue or SemanticNumberValue } value => value.Value!,
        _ => throw new InvalidSemanticContract($"Capture child identity '{field}' must be present as text or a number.")
    };

    static Dictionary<SemanticValue, SemanticCaptureRecord> Index(System.Collections.Immutable.ImmutableArray<SemanticCaptureRecord> records, string field)
    {
        var index = new Dictionary<SemanticValue, SemanticCaptureRecord>();
        foreach (var record in records)
        {
            if (!index.TryAdd(Identity(record, field), record)) throw new InvalidSemanticContract($"Capture children have duplicate identity '{field}'.");
        }
        return index;
    }

    static SemanticRunReactionFailure UnsupportedKey(SemanticCapture capture) =>
        SemanticRunReactionFailure.Unsupported($"Capture '{capture.Name}' requires a text or numeric event key '{capture.Key}'.");

    static SemanticCaptureField EventField(string name, SemanticValue value, IReadOnlyDictionary<SemanticId, string> names) => value switch
    {
        SemanticArrayValue { Values.IsDefaultOrEmpty: false } array when array.Values.All(element => element is SemanticCompositeValue) =>
            new(name, SemanticCaptureFieldKind.Records) { Value = value, Records = [.. array.Values.Cast<SemanticCompositeValue>().Select(element => EventObject(element, names))] },
        SemanticCompositeValue composite => new(name, SemanticCaptureFieldKind.Record) { Value = value, Record = EventObject(composite, names) },
        _ => new(name, SemanticCaptureFieldKind.Value) { Value = value }
    };

    static SemanticCaptureRecord EventObject(SemanticCompositeValue composite, IReadOnlyDictionary<SemanticId, string> names) =>
        new([.. composite.Properties.Select(property => EventField(names[property.TargetProperty], property.Value, names))]);

    static SemanticCaptureRecord Overlay(SemanticCaptureRecord? previous, SemanticCaptureRecord current)
    {
        var fields = previous?.Fields.ToDictionary(field => field.Name, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
        foreach (var field in current.Fields) fields[field.Name] = field;
        return new([.. fields.Values]);
    }
    IEnumerable<SemanticCapture> Matching(SemanticSpecificationEvent fact) =>
        plan.Captures.Values.Where(capture => SemanticTranslationSupport.Capture(capture, plan.Events) && capture.EventsSource!.Events.Contains(fact.EventContract))
            .OrderBy(capture => capture.Id.ToString(), StringComparer.Ordinal);

    SemanticCaptureRecord EventRecord(SemanticSpecificationEvent fact)
    {
        var contract = plan.Events[fact.EventContract];
        var names = plan.Model.Application.Types.SelectMany(type => type.Properties).ToDictionary(property => property.Id, property => property.Name);
        return new([.. fact.Values.Select(value => EventField(contract.Properties.Single(property => property.Id == value.TargetProperty).Name, value.Value, names))]);
    }

    SemanticValue EventKey(SemanticCapture capture, SemanticSpecificationEvent fact, SemanticCaptureRecord record)
    {
        var key = record.Fields.SingleOrDefault(field => string.Equals(field.Name, capture.Key, StringComparison.Ordinal))?.Value;
        if (key is null or SemanticNullValue || (key is SemanticTextValue text && string.IsNullOrWhiteSpace(text.Value)))
        {
            throw new InvalidSemanticContract($"Capture '{capture.Name}' received event '{plan.Events[fact.EventContract].Name}' with an empty key '{capture.Key}'.");
        }
        return key;
    }
}
