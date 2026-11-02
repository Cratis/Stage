// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles;

/// <summary>
/// Renders guards over the followed read model and the arguments of the commands a lifecycle sends.
/// </summary>
internal static class SemanticLifecycleExpressions
{
    internal static bool CanRender(SemanticCondition condition, SemanticReadModel model) => condition switch
    {
        SemanticLogicalCondition logical => CanRender(logical.Left, model) && CanRender(logical.Right, model),
        SemanticComparison comparison => CanRender(comparison.Left, model) && CanRender(comparison.Right, model),
        _ => false
    };

    internal static string Guard(SemanticCondition condition, SemanticReadModel model, SemanticTypeSystem types) => condition switch
    {
        SemanticLogicalCondition logical => $"({Guard(logical.Left, model, types)} {(logical.Operator == SemanticLogicalOperator.And ? "&&" : "||")} {Guard(logical.Right, model, types)})",
        SemanticComparison comparison => $"{Operand(comparison.Left, model, types, comparison.Right)} {Operator(comparison.Operator)} {Operand(comparison.Right, model, types, comparison.Left)}",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCondition), condition)
    };

    /// <summary>
    /// Finds a command property the lifecycle cannot fill: neither the identifier nor a member of the read model with the same name and type.
    /// </summary>
    /// <param name="context">The application.</param>
    /// <param name="command">The command.</param>
    /// <param name="model">The read model the lifecycle follows.</param>
    /// <returns>The property name, or null when every property can be filled.</returns>
    internal static string? UnmappedProperty(SemanticApplicationContext context, SemanticCommand command, SemanticReadModel model)
    {
        _ = context;
        foreach (var property in command.Properties.Where(property => !property.IsGenerated))
        {
            if (property.IsIdentifier)
            {
                if (!CanConvertKey(property.Type)) return property.Name;
                continue;
            }

            if (!model.Properties.Any(member => string.Equals(member.Name, property.Name, StringComparison.OrdinalIgnoreCase) && member.Type == property.Type)) return property.Name;
        }

        return null;
    }

    /// <summary>
    /// Renders the argument list of a command a state machine sends when it enters a state.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="model">The followed read model.</param>
    /// <param name="types">The type system.</param>
    /// <param name="key">The expression holding the instance key as text.</param>
    /// <param name="state">The expression holding the read model.</param>
    /// <returns>The comma separated arguments.</returns>
    internal static string Arguments(SemanticCommand command, SemanticReadModel model, SemanticTypeSystem types, string key, string state) =>
        string.Join(", ", command.Properties.Where(property => !property.IsGenerated).Select(property => property.IsIdentifier
            ? Key(property.Type, key, types)
            : $"{state}.{Identifiers.ToPascalCase(model.Properties.First(member => string.Equals(member.Name, property.Name, StringComparison.OrdinalIgnoreCase) && member.Type == property.Type).Name)}"));

    internal static string Key(SemanticTypeReference type, string key, SemanticTypeSystem types)
    {
        var primitive = type.Kind == SemanticTypeReferenceKind.Concept ? types.ConceptPrimitive(type) : type.Primitive;
        var value = primitive == SemanticPrimitiveType.Uuid ? $"global::System.Guid.Parse({key})" : key;
        return type.Kind == SemanticTypeReferenceKind.Concept ? $"new {types.Type(type with { IsOptional = false })}({value})" : value;
    }

    static bool CanConvertKey(SemanticTypeReference type) => !type.IsCollection && !type.IsOptional && type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => type.Primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid,
        SemanticTypeReferenceKind.Concept => true,
        _ => false
    };

    static bool CanRender(SemanticConditionOperand operand, SemanticReadModel model) => operand.Read is null && (operand.Value is { } value
        ? value is SemanticTextValue or SemanticNumberValue or SemanticBooleanValue
        : model.Properties.FirstOrDefault(property => property.Id == operand.Property) is { Type: { Kind: SemanticTypeReferenceKind.Primitive, IsCollection: false, IsOptional: false } type } &&
          type.Primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Boolean or SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber);

    static string Operand(SemanticConditionOperand operand, SemanticReadModel model, SemanticTypeSystem types, SemanticConditionOperand other)
    {
        if (operand.Value is { } value)
        {
            var type = model.Properties.First(property => property.Id == other.Property).Type;
            return types.Value(value, type);
        }

        return $"model.{Identifiers.ToPascalCase(model.Properties.First(property => property.Id == operand.Property).Name)}";
    }

    static string Operator(SemanticComparisonOperator comparison) => comparison switch
    {
        SemanticComparisonOperator.Equal => "==",
        SemanticComparisonOperator.NotEqual => "!=",
        SemanticComparisonOperator.GreaterThan => ">",
        SemanticComparisonOperator.GreaterThanOrEqual => ">=",
        SemanticComparisonOperator.LessThan => "<",
        SemanticComparisonOperator.LessThanOrEqual => "<=",
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticComparisonOperator), comparison)
    };
}
