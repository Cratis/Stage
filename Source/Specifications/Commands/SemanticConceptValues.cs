// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands;

/// <summary>
/// Validates supplied concepts recursively in reference order, without inventing absent members.
/// </summary>
internal static class SemanticConceptValues
{
    internal static IEnumerable<SemanticValidationFailure> Validate(
        SemanticTypeReference type,
        SemanticValue value,
        IReadOnlyDictionary<SemanticId, SemanticConcept> concepts,
        IReadOnlyDictionary<SemanticId, SemanticCompositeType> composites)
    {
        if (type.IsCollection)
        {
            return value is SemanticArrayValue array
                ? array.Values.SelectMany(element => Validate(type with { IsCollection = false, IsOptional = false }, element, concepts, composites))
                : [];
        }

        return type.Kind switch
        {
            SemanticTypeReferenceKind.Concept => concepts[type.Target].Validations
                .Where(rule => !SemanticRuleEvaluation.Satisfies(rule, value))
                .Select(rule => new SemanticValidationFailure(rule.Message ?? SemanticRuleEvaluation.Message(rule, value, "A required concept value is empty."), rule.Severity)),
            SemanticTypeReferenceKind.CompositeType when value is SemanticCompositeValue composite => composite.Properties
                .SelectMany(property => Validate(composites[type.Target].Properties.Single(member => member.Id == property.TargetProperty).Type, property.Value, concepts, composites)),
            _ => []
        };
    }

    // Compile indexes capability-admitted declarations; it does not validate typed fixture values.
    // Match SemanticEvaluator.ValidateRequest before command rules, not only for reaction invocations.
    internal static string? RequestFailure(SemanticExecutionPlan plan, SemanticCommand command, ImmutableArray<SemanticPropertyValue> values)
    {
        if (values.IsDefault || values.Any(value => value is null) || values.Length != command.Properties.Count(property => !property.IsGenerated) ||
            values.Select(value => value.TargetProperty).Distinct().Count() != values.Length)
        {
            return $"Command '{command.Name}' values do not match its exact property shape.";
        }
        var supplied = values.ToDictionary(value => value.TargetProperty);
        foreach (var property in command.Properties.Where(property => !property.IsGenerated))
        {
            if (!supplied.TryGetValue(property.Id, out var value)) return $"Command '{command.Name}' is missing property '{property.Name}'.";
            try
            {
                ValidateShape(plan, value.Value, property.Type, $"command property '{property.Name}'", 0);
            }
            catch (InvalidSemanticContract exception)
            {
                return exception.Message;
            }
        }

        return null;
    }

    static void ValidateShape(SemanticExecutionPlan plan, SemanticValue value, SemanticTypeReference type, string description, int depth)
    {
        if (depth >= 64) throw new InvalidSemanticContract("A semantic value exceeds the canonical maximum depth of 64.");
        ValidateNode(value);
        if (value is SemanticNullValue)
        {
            if (!type.IsOptional) throw new InvalidSemanticContract($"A null {description} is incompatible with a required target.");
            return;
        }
        if (type.IsCollection)
        {
            if (value is not SemanticArrayValue array) throw new InvalidSemanticContract($"A scalar {description} is incompatible with a collection target.");
            foreach (var element in array.Values) ValidateShape(plan, element, type with { IsCollection = false, IsOptional = false }, $"element of {description}", depth + 1);
            return;
        }
        if (value is SemanticArrayValue) throw new InvalidSemanticContract($"A collection {description} is incompatible with a scalar target.");
        if (type.Kind != SemanticTypeReferenceKind.CompositeType)
        {
            SemanticRunReactionExpressions.Validate(plan, value, type, description);
            return;
        }
        if (value is not SemanticCompositeValue composite) throw new InvalidSemanticContract($"A {description} is incompatible with its composite target type.");
        var definition = plan.Model.Application.Types.Single(candidate => candidate.Id == type.Target);
        foreach (var member in composite.Properties)
        {
            var property = definition.Properties.SingleOrDefault(property => property.Id == member.TargetProperty)
                ?? throw new InvalidSemanticContract($"Composite value property '{member.TargetProperty}' is unknown for '{definition.Name}'.");
            ValidateShape(plan, member.Value, property.Type, $"property '{property.Name}' of {description}", depth + 1);
        }
        if (definition.Properties.Any(property => !property.Type.IsOptional && !composite.Properties.Any(member => member.TargetProperty == property.Id)))
        {
            throw new InvalidSemanticContract($"A {description} is missing one or more required properties of '{definition.Name}'.");
        }
    }

    static void ValidateNode(SemanticValue value)
    {
        var valid = value switch
        {
            SemanticNullValue => value.Kind == SemanticValueKind.Null,
            SemanticTextValue text => value.Kind == SemanticValueKind.Text && text.Value is not null,
            SemanticNumberValue => value.Kind == SemanticValueKind.Number,
            SemanticBooleanValue => value.Kind == SemanticValueKind.Boolean,
            SemanticArrayValue array => value.Kind == SemanticValueKind.Array && !array.Values.IsDefault && array.Values.All(element => element is not null),
            SemanticCompositeValue composite => value.Kind == SemanticValueKind.Composite && !composite.Properties.IsDefault &&
                composite.Properties.All(property => property is { TargetProperty.IsSet: true, Value: not null }) &&
                composite.Properties.Select(property => property.TargetProperty).Distinct().Count() == composite.Properties.Length,
            _ => false
        };
        if (!valid) throw new InvalidSemanticContract("A semantic value variant is malformed or unknown.");
    }
}
