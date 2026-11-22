// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Classifies the required scalar UTC now condition subset without admitting calendar or validation clocks.
/// </summary>
internal static class SemanticCommandClockSupport
{
    internal static bool Uses(SemanticCommand command) => command.Calendar is not null ||
        command.Validations.Any(rule => rule.ClockOperand is not null) || Conditions(command).Any(Uses);

    internal static bool Uses(SemanticCondition condition) => condition switch
    {
        SemanticLogicalCondition logical => Uses(logical.Left) || Uses(logical.Right),
        SemanticComparison comparison => Uses(comparison.Left) || Uses(comparison.Right),
        _ => false
    };

    internal static bool Supports(SemanticApplication application, SemanticCommand command) =>
        (application.TimeZone is null || (string.Equals(application.TimeZone.Id, "UTC", StringComparison.Ordinal) && !application.TimeZone.Configurable)) && application.ContextMembers.IsEmpty && command.Calendar is null &&
        command.Validations.All(rule => rule.ClockOperand is null) &&
        Conditions(command).All(condition => Supports(condition, command.Properties, application));

    internal static bool Supports(SemanticCondition condition, IReadOnlyList<SemanticProperty> properties, SemanticApplication application) => condition switch
    {
        SemanticLogicalCondition logical => Enum.IsDefined(logical.Operator) &&
            Supports(logical.Left, properties, application) && Supports(logical.Right, properties, application),
        SemanticComparison comparison => !Uses(comparison) || Comparison(comparison, properties, application),
        _ => false
    };

    internal static bool Comparison(SemanticComparison comparison, IReadOnlyList<SemanticProperty> properties, SemanticApplication application) =>
        Ordered(comparison.Operator) &&
        ((Clock(comparison.Left) && Property(comparison.Right, properties, application)) ||
         (Property(comparison.Left, properties, application) && Clock(comparison.Right)));

    static IEnumerable<SemanticCondition> Conditions(SemanticCommand command) =>
        command.Requirements.Select(requirement => requirement.Condition)
            .Concat(command.Produces.Select(produced => produced.When).OfType<SemanticCondition>());

    static bool Uses(SemanticConditionOperand operand) => operand.Clock is not null || operand.DateOf;

    static bool Clock(SemanticConditionOperand operand) =>
        operand is { DateOf: false, Read: null, CallerId: false, Clock: { Kind: SemanticClockKind.Now, DateOf: false } clock } &&
        Enum.IsDefined(clock.Unit);

    static bool Property(SemanticConditionOperand operand, IReadOnlyList<SemanticProperty> properties, SemanticApplication application)
    {
        if (operand is not { Clock: null, DateOf: false, Read: null, CallerId: false, Value: null }) return false;
        var type = properties.FirstOrDefault(property => property.Id == operand.Property && !property.IsGenerated)?.Type;
        if (type is null or { IsOptional: true } or { IsCollection: true }) return false;
        if (type.Kind == SemanticTypeReferenceKind.Primitive) return type.Primitive == SemanticPrimitiveType.DateTime;
        return type.Kind == SemanticTypeReferenceKind.Concept && application.Concepts.Any(concept =>
            concept.Id == type.Target && concept.Primitive == SemanticPrimitiveType.DateTime && concept.Values.IsEmpty && concept.Compliance is null);
    }

    static bool Ordered(SemanticComparisonOperator operation) => operation is
        SemanticComparisonOperator.Equal or SemanticComparisonOperator.NotEqual or
        SemanticComparisonOperator.LessThan or SemanticComparisonOperator.LessThanOrEqual or
        SemanticComparisonOperator.GreaterThan or SemanticComparisonOperator.GreaterThanOrEqual;
}
