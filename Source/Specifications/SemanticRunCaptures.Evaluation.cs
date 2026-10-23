// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications;

internal sealed partial class SemanticRunCaptures
{
    static string Text(SemanticValue? value) => value switch
    {
        SemanticTextValue text => text.Value,
        SemanticNumberValue number => number.Value.ToString("0.############################", CultureInfo.InvariantCulture),
        SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
        _ => string.Empty
    };

    bool SameState(SemanticCaptureRecord previous, SemanticCaptureRecord current)
    {
        var before = previous.Fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
        return before.Count == current.Fields.Length && current.Fields.All(field => before.TryGetValue(field.Name, out var earlier) && SameValue(earlier.Value!, field.Value!));
    }

    bool SameValue(SemanticValue previous, SemanticValue current) => (previous, current) switch
    {
        (SemanticCompositeValue before, SemanticCompositeValue after) => SameObject(before, after),
        (SemanticArrayValue before, SemanticArrayValue after) => before.Values.Length == after.Values.Length && before.Values.Zip(after.Values).All(pair => SameValue(pair.First, pair.Second)),
        _ => SemanticExpectationComparer.AreEqual(previous, current)
    };

    bool SameObject(SemanticCompositeValue before, SemanticCompositeValue after)
    {
        var names = plan.Model.Application.Types.SelectMany(type => type.Properties).ToDictionary(property => property.Id, property => property.Name);
        var properties = before.Properties.ToDictionary(property => names[property.TargetProperty], property => property.Value, StringComparer.Ordinal);
        return properties.Count == after.Properties.Length && after.Properties.All(property => properties.TryGetValue(names[property.TargetProperty], out var earlier) && SameValue(earlier, property.Value));
    }

    // Evaluates one record, or one child of it: the child reads its own fields and falls back to the enclosing record's.
    // A null current is a child that is no longer there, which only a removed condition holds for.
    private sealed partial class Evaluation(SemanticExecutionPlan plan, SemanticCapture capture, SemanticValue key, SemanticCaptureRecord? previous, SemanticCaptureRecord? current, SemanticCaptureRecord? enclosing = null)
    {
        readonly Dictionary<string, SemanticCaptureField>? _before = previous?.Fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
        readonly Dictionary<string, SemanticCaptureField> _after = current?.Fields.ToDictionary(field => field.Name, StringComparer.Ordinal) ?? [];
        readonly Dictionary<string, SemanticCaptureField>? _enclosing = enclosing?.Fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
        internal string? Unsupported { get; private set; }

        internal void ValidateSources(System.Collections.Immutable.ImmutableArray<SemanticCaptureAppend> appends)
        {
            foreach (var append in appends)
            {
                var type = append.EventSourceType;
                var primitive = type.Kind == SemanticTypeReferenceKind.Concept ? plan.Model.Application.Concepts.Single(concept => concept.Id == type.Target).Primitive : type.Primitive;
                if (key is SemanticNumberValue && primitive == SemanticPrimitiveType.Text)
                {
                    throw new InvalidSemanticContract($"Capture '{capture.Name}' key '{capture.Key}' is {Text(key)} (number); event '{plan.Events[append.EventContract].Name}' has a text event source, so the key must be text: write \"{Text(key)}\".");
                }
                Validate(key, type, "capture event source");
            }
        }

        internal bool Holds(SemanticCaptureCondition? when) => when?.Kind switch
        {
            null => current is not null,
            SemanticCaptureConditionKind.Added => current is not null && _before is null,
            SemanticCaptureConditionKind.Removed => current is null,
            _ when current is null => false,
            SemanticCaptureConditionKind.AnyChanged or SemanticCaptureConditionKind.AllChanged or SemanticCaptureConditionKind.Transition when _before is null => FirstObservation(when),
            SemanticCaptureConditionKind.AnyChanged => when.Fields.Any(field => Unsupported is null && Changed(field)),
            SemanticCaptureConditionKind.AllChanged => when.Fields.All(field => Unsupported is null && Changed(field)),
            SemanticCaptureConditionKind.Transition => Transition(when),
            SemanticCaptureConditionKind.Expression => Expression(when.Expression),
            _ => throw new InvalidSemanticContract($"Capture condition '{when.Kind}' is unknown.")
        };

        bool FirstObservation(SemanticCaptureCondition when)
        {
            foreach (var field in when.Fields)
            {
                if (Read(_after, field).Unsupported is not null) return false;
            }
            return false;
        }

        bool Expression(string? text)
        {
            if (!SemanticRunCaptureExpression.TryParse(text, out var expression)) return false;
            var result = SemanticRunCaptureExpression.Evaluate(expression, field => Read(_after, field));
            Unsupported ??= result.Unsupported;
            return result.Holds;
        }

        bool Transition(SemanticCaptureCondition when)
        {
            var was = Read(_before, when.Fields[0]);
            if (was.Unsupported is not null || Text(was.Value) != when.From) return false;
            var now = Read(_after, when.Fields[0]);
            return now.Unsupported is null && Text(now.Value) == when.To;
        }

        bool Changed(string field)
        {
            var was = Read(_before, field);
            if (was.Unsupported is not null) return false;
            var now = Read(_after, field);
            if (now.Unsupported is not null) return false;
            return _before!.ContainsKey(field) != _after.ContainsKey(field) ||
                (was.Value is not null && now.Value is not null && !SemanticExpectationComparer.AreEqual(was.Value, now.Value));
        }

        SemanticRunCaptureExpression.Lookup Read(Dictionary<string, SemanticCaptureField>? record, string field, bool structured = false)
        {
            if (field.Contains('.', StringComparison.Ordinal))
            {
                Unsupported ??= $"Capture path '{field}' requires traversal the reference evaluator does not support.";
                return new(null, Unsupported);
            }
            var present = record?.GetValueOrDefault(field) ?? (_enclosing is not null ? _enclosing.GetValueOrDefault(field) : null);
            if (structured && present is { Value: SemanticCompositeValue or SemanticArrayValue }) return new(present.Value, null);
            if (present is null or { Kind: SemanticCaptureFieldKind.Value, Value: not (SemanticCompositeValue or SemanticArrayValue) }) return new(present?.Value, null);
            Unsupported ??= $"Capture field '{field}' is a {present.Kind} value the reference evaluator cannot use as a scalar.";
            return new(null, Unsupported);
        }
    }
}
