// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications;

internal sealed partial class SemanticRunCaptures
{
    private sealed partial class Evaluation
    {
        [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.NonBacktracking)]
        private static partial Regex IsoInstant { get; }

        internal SemanticSpecificationEvent Produce(SemanticCaptureAppend append)
        {
            var contract = plan.Events[append.EventContract];
            var values = new List<SemanticPropertyValue>();
            foreach (var mapping in append.Mappings)
            {
                var property = contract.Properties.Single(candidate => candidate.Id == mapping.TargetProperty);
                var source = mapping.Field is { } field ? Read(_after, field, structured: true) : new SemanticRunCaptureExpression.Lookup(((SemanticValueExpression)mapping.Value!).Value, null);
                if (source.Unsupported is not null) break;
                var value = source.Value ?? SemanticValue.Null;
                if (mapping.Field is not null) value = Retarget(value, property.Type, property.Name);
                Validate(value, property.Type, $"event property '{property.Name}'");
                values.Add(new(property.Id, value));
            }
            Validate(key, append.EventSourceType, "capture event source");
            return new(append.EventContract, [.. values]) { EventSource = new(append.EventSourceType, key) };
        }

        SemanticValue Retarget(SemanticValue value, SemanticTypeReference target, string where)
        {
            if (value is SemanticNullValue) return value;
            if (target.IsCollection)
            {
                return value is SemanticArrayValue array
                    ? new SemanticArrayValue([.. array.Values.Select(element => Retarget(element, target with { IsCollection = false, IsOptional = false }, where))])
                    : throw new InvalidSemanticContract($"Event property '{where}' is a list, but the captured value is not.");
            }
            if (target.Kind != SemanticTypeReferenceKind.CompositeType) return Coerce(value, target);
            if (value is not SemanticCompositeValue composite) throw new InvalidSemanticContract($"Event property '{where}' is an object, but the captured value is not.");
            var names = plan.Model.Application.Types.SelectMany(type => type.Properties).ToDictionary(property => property.Id, property => property.Name);
            var type = plan.Model.Application.Types.Single(candidate => candidate.Id == target.Target);
            var byName = type.Properties.ToDictionary(property => property.Name, StringComparer.Ordinal);
            var written = new List<SemanticPropertyValue>();
            foreach (var entry in composite.Properties)
            {
                var name = names.GetValueOrDefault(entry.TargetProperty) ?? throw new InvalidSemanticContract($"Event property '{where}' holds an unknown captured property.");
                if (!byName.TryGetValue(name, out var property)) throw new InvalidSemanticContract($"Event property '{where}' has no property '{name}' in '{type.Name}', so the captured value cannot be copied.");
                written.Add(new(property.Id, Retarget(entry.Value, property.Type, $"{where}.{name}")));
            }
            return new SemanticCompositeValue([.. written]);
        }

        SemanticValue Coerce(SemanticValue value, SemanticTypeReference type)
        {
            var primitive = type.Kind == SemanticTypeReferenceKind.Concept ? plan.Model.Application.Concepts.Single(concept => concept.Id == type.Target).Primitive : type.Primitive;
            if (primitive != SemanticPrimitiveType.DateTime || value is not SemanticTextValue text) return value;
            if (!IsoInstant.IsMatch(text.Value) || !DateTimeOffset.TryParse(text.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant))
            {
                throw new InvalidSemanticContract("A capture DateTime field must contain a complete ISO date and time with an explicit zone.");
            }
            return SemanticValue.Text(instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        }

        void Validate(SemanticValue value, SemanticTypeReference type, string description)
        {
            if (value is not SemanticNullValue && type.IsCollection)
            {
                if (value is not SemanticArrayValue array) throw new InvalidSemanticContract($"A scalar {description} is incompatible with a collection target.");
                foreach (var element in array.Values) Validate(element, type with { IsCollection = false, IsOptional = false }, $"element of {description}");
                return;
            }
            SemanticRunReactionExpressions.Validate(plan, value, type, description);
        }
    }
}
