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
        IReadOnlyDictionary<SemanticId, SemanticCompositeType> composites) =>
        Validate(type, value, new ConceptContext(concepts, composites));

    internal static IEnumerable<SemanticValidationFailure> Validate(SemanticTypeReference type, SemanticValue value, SemanticRunContext run) =>
        Validate(type, value, new ConceptContext(run.Plan.Model.Application.Concepts.ToDictionary(concept => concept.Id), run.Plan.Model.Application.Types.ToDictionary(composite => composite.Id)) { Run = run });

    // Compile indexes capability-admitted declarations; it does not validate typed fixture values.
    // Match SemanticEvaluator.ValidateRequest before command rules, not only for reaction invocations.
    internal static string? RequestFailure(SemanticExecutionPlan plan, SemanticCommand command, ImmutableArray<SemanticPropertyValue> values)
    {
        if (HasMalformedRequest(command, values)) return $"Command '{command.Name}' values do not match its exact property shape.";
        var supplied = values.ToDictionary(value => value.TargetProperty);

        return command.Properties.Where(property => !property.IsGenerated)
            .Select(property => PropertyFailure(plan, command, property, supplied))
            .FirstOrDefault(failure => failure is not null);
    }

    static IEnumerable<SemanticValidationFailure> Validate(SemanticTypeReference type, SemanticValue value, ConceptContext context)
    {
        if (type.IsCollection) return ValidateElements(type, value, context);

        return type.Kind switch
        {
            SemanticTypeReferenceKind.Concept => ConceptFailures(context.Concepts[type.Target], value, context.Run),
            SemanticTypeReferenceKind.CompositeType => CompositeFailures(type, value, context),
            _ => []
        };
    }

    static IEnumerable<SemanticValidationFailure> ValidateElements(SemanticTypeReference type, SemanticValue value, ConceptContext context) =>
        value is SemanticArrayValue array
            ? array.Values.SelectMany(element => Validate(type with { IsCollection = false, IsOptional = false }, element, context))
            : [];

    static IEnumerable<SemanticValidationFailure> ConceptFailures(SemanticConcept concept, SemanticValue value, SemanticRunContext? run) =>
        concept.Validations.Select(rule => SemanticRuleEvaluation.ConceptCheck(rule, value, run)).Where(check => !SemanticRuleEvaluation.Satisfies(check))
            .Select(check => new SemanticValidationFailure(check.Rule.Message ?? (check.Rule.Kind == SemanticValidationRuleKind.NotEmpty ? "A required concept value is empty." : check.Message), check.Rule.Severity));

    static IEnumerable<SemanticValidationFailure> CompositeFailures(SemanticTypeReference type, SemanticValue value, ConceptContext context) =>
        value is SemanticCompositeValue composite
            ? composite.Properties.SelectMany(property => Validate(context.Composites[type.Target].Properties.Single(member => member.Id == property.TargetProperty).Type, property.Value, context))
            : [];

    static bool HasMalformedRequest(SemanticCommand command, ImmutableArray<SemanticPropertyValue> values)
    {
        if (values.IsDefault) return true;
        if (values.Any(value => value is null)) return true;
        if (values.Length != command.Properties.Count(property => !property.IsGenerated)) return true;

        return values.Select(value => value.TargetProperty).Distinct().Count() != values.Length;
    }

    static string? PropertyFailure(SemanticExecutionPlan plan, SemanticCommand command, SemanticProperty property, Dictionary<SemanticId, SemanticPropertyValue> supplied)
    {
        if (!supplied.TryGetValue(property.Id, out var value)) return $"Command '{command.Name}' is missing property '{property.Name}'.";
        try
        {
            ValidateShape(value.Value, property.Type, new ShapeContext(plan, $"command property '{property.Name}'", 0));
        }
        catch (InvalidSemanticContract exception)
        {
            return exception.Message;
        }

        return null;
    }

    static void ValidateShape(SemanticValue value, SemanticTypeReference type, ShapeContext context)
    {
        if (context.Depth >= 64) throw new InvalidSemanticContract("A semantic value exceeds the canonical maximum depth of 64.");
        ValidateNode(value);
        if (value is SemanticNullValue)
        {
            ValidateNull(type, context);
            return;
        }
        if (type.IsCollection)
        {
            ValidateArray(value, type, context);
            return;
        }
        ValidateSingle(value, type, context);
    }

    static void ValidateNull(SemanticTypeReference type, ShapeContext context)
    {
        if (!type.IsOptional) throw new InvalidSemanticContract($"A null {context.Description} is incompatible with a required target.");
    }

    static void ValidateArray(SemanticValue value, SemanticTypeReference type, ShapeContext context)
    {
        if (value is not SemanticArrayValue array) throw new InvalidSemanticContract($"A scalar {context.Description} is incompatible with a collection target.");
        var elementType = type with { IsCollection = false, IsOptional = false };
        foreach (var element in array.Values) ValidateShape(element, elementType, context.Descend($"element of {context.Description}"));
    }

    static void ValidateSingle(SemanticValue value, SemanticTypeReference type, ShapeContext context)
    {
        if (value is SemanticArrayValue) throw new InvalidSemanticContract($"A collection {context.Description} is incompatible with a scalar target.");
        if (type.Kind == SemanticTypeReferenceKind.CompositeType)
        {
            ValidateComposite(value, type, context);
            return;
        }
        SemanticRunReactionExpressions.Validate(context.Plan, value, type, context.Description);
    }

    static void ValidateComposite(SemanticValue value, SemanticTypeReference type, ShapeContext context)
    {
        if (value is not SemanticCompositeValue composite) throw new InvalidSemanticContract($"A {context.Description} is incompatible with its composite target type.");
        var definition = context.Plan.Model.Application.Types.Single(candidate => candidate.Id == type.Target);
        foreach (var member in composite.Properties) ValidateMember(member, definition, context);
        if (HasMissingRequiredMembers(definition, composite))
        {
            throw new InvalidSemanticContract($"A {context.Description} is missing one or more required properties of '{definition.Name}'.");
        }
    }

    static void ValidateMember(SemanticPropertyValue member, SemanticCompositeType definition, ShapeContext context)
    {
        var property = definition.Properties.SingleOrDefault(property => property.Id == member.TargetProperty)
            ?? throw new InvalidSemanticContract($"Composite value property '{member.TargetProperty}' is unknown for '{definition.Name}'.");
        ValidateShape(member.Value, property.Type, context.Descend($"property '{property.Name}' of {context.Description}"));
    }

    static bool HasMissingRequiredMembers(SemanticCompositeType definition, SemanticCompositeValue composite) =>
        definition.Properties.Any(property => IsMissingRequiredMember(property, composite));

    static bool IsMissingRequiredMember(SemanticProperty property, SemanticCompositeValue composite) =>
        !property.Type.IsOptional && !composite.Properties.Any(member => member.TargetProperty == property.Id);

    static void ValidateNode(SemanticValue value)
    {
        if (!IsValidNode(value)) throw new InvalidSemanticContract("A semantic value variant is malformed or unknown.");
    }

    static bool IsValidNode(SemanticValue value) => value switch
    {
        SemanticNullValue => value.Kind == SemanticValueKind.Null,
        SemanticTextValue text => IsValidText(text),
        SemanticNumberValue => value.Kind == SemanticValueKind.Number,
        SemanticBooleanValue => value.Kind == SemanticValueKind.Boolean,
        SemanticArrayValue array => IsValidArray(array),
        SemanticCompositeValue composite => IsValidComposite(composite),
        _ => false
    };

    static bool IsValidText(SemanticTextValue text) => text.Kind == SemanticValueKind.Text && text.Value is not null;

    static bool IsValidArray(SemanticArrayValue array) =>
        array.Kind == SemanticValueKind.Array && HasValidElements(array.Values);

    static bool HasValidElements(ImmutableArray<SemanticValue> values) => !values.IsDefault && values.All(value => value is not null);

    static bool IsValidComposite(SemanticCompositeValue composite) =>
        composite.Kind == SemanticValueKind.Composite && HasValidMembers(composite.Properties);

    static bool HasValidMembers(ImmutableArray<SemanticPropertyValue> properties)
    {
        if (properties.IsDefault) return false;
        if (!properties.All(property => property is { TargetProperty.IsSet: true, Value: not null })) return false;

        return properties.Select(property => property.TargetProperty).Distinct().Count() == properties.Length;
    }

    sealed record ConceptContext(
        IReadOnlyDictionary<SemanticId, SemanticConcept> Concepts,
        IReadOnlyDictionary<SemanticId, SemanticCompositeType> Composites)
    {
        internal SemanticRunContext? Run { get; init; }
    }

    sealed record ShapeContext(SemanticExecutionPlan Plan, string Description, int Depth)
    {
        internal ShapeContext Descend(string description) => this with { Description = description, Depth = Depth + 1 };
    }
}
