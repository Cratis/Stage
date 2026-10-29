// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Rendering.Model;

internal sealed class RenderExpressionNormalizer(RenderTypeResolver types, SemanticExpressionRootKind root = SemanticExpressionRootKind.Command)
{
    internal static RenderTypeRef BooleanType() => new(RenderTypeKind.Primitive, SemanticPrimitiveType.Boolean, null, false, false);

    internal RenderExpression Normalize(SemanticExpression expression, RenderTypeRef? contextualType = null) => expression switch
    {
        SemanticValueExpression value => new RenderExpression.Value(Literal(value.Value), contextualType),
        SemanticResolvedExpression value when value.Root is SemanticExpressionRootKind.Command or SemanticExpressionRootKind.Event or SemanticExpressionRootKind.Trigger && value.Source == SemanticExpressionSourceKind.Property => new RenderExpression.Resolved(value.Root, value.Source, value.Target, types.Property(value.Target)),
        SemanticEventContextExpression value when value.Value != SemanticEventContextValueKind.Unknown && Enum.IsDefined(value.Value) => new RenderExpression.EventContext(value.Value, types.Resolve(value.Type)),
        SemanticRefusalExpression value => new RenderExpression.Refusal(value.Member, types.Resolve(value.Type)),
        SemanticReadPathExpression value => new RenderExpression.ReadPath(value.Read, value.PropertyId, types.Property(value.PropertyId)),
        _ => throw new InvalidArtifactRenderContract($"Unclassified expression '{expression.Kind}'{(expression is SemanticResolvedExpression resolved ? $" targeting '{resolved.Target}'" : string.Empty)}.")
    };

    internal RenderExpression Condition(SemanticCondition condition) => condition switch
    {
        SemanticComparison value when Enum.IsDefined(value.Operator) => new RenderExpression.Comparison(Operand(value.Left), value.Operator, Operand(value.Right), BooleanType()),
        SemanticLogicalCondition value when Enum.IsDefined(value.Operator) => new RenderExpression.Logical(Condition(value.Left), value.Operator, Condition(value.Right), BooleanType()),
        _ => throw new InvalidArtifactRenderContract($"Unclassified condition '{condition.GetType().Name}'.")
    };

    internal RenderLiteral Literal(SemanticValue value) => value switch
    {
        SemanticNullValue => new RenderLiteral.Null(),
        SemanticTextValue text => new RenderLiteral.Text(text.Value),
        SemanticNumberValue number => new RenderLiteral.Number(number.Value),
        SemanticBooleanValue boolean => new RenderLiteral.Boolean(boolean.Value),
        SemanticArrayValue array => new RenderLiteral.Array(new(array.Values.Select(Literal))),
        SemanticCompositeValue composite => new RenderLiteral.Composite(new(composite.Properties.OrderBy(property => property.TargetProperty.ToString(), StringComparer.Ordinal).Select(property => new RenderLiteralProperty(property.TargetProperty, Literal(property.Value))))),
        _ => throw new InvalidArtifactRenderContract($"Unclassified literal '{value.Kind}'.")
    };

    RenderExpression Operand(SemanticConditionOperand operand) => operand switch
    {
        { Read: not null } => new RenderExpression.ReadPath(operand.Read, operand.Property, types.Property(operand.Property)),
        { Property.IsSet: true } => new RenderExpression.Resolved(root, SemanticExpressionSourceKind.Property, operand.Property, types.Property(operand.Property)),
        { Value: not null } => new RenderExpression.Value(Literal(operand.Value), null),
        _ => throw new InvalidArtifactRenderContract($"Unclassified condition operand targeting '{operand.Property}'.")
    };
}
