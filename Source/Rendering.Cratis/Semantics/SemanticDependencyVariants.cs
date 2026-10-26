// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Refuses to claim completeness for future expression, type or context variants.</summary>
internal static class SemanticDependencyVariants
{
    static readonly HashSet<string> _variants = new(StringComparer.Ordinal)
    {
        "SemanticResolvedExpression", "SemanticValueExpression", "SemanticEventContextExpression", "SemanticRefusalExpression", "SemanticReadPathExpression",
        "SemanticTextValue", "SemanticNumberValue", "SemanticBooleanValue", "SemanticNullValue", "SemanticArrayValue", "SemanticCompositeValue",
        "SemanticComparison", "SemanticLogicalCondition", "SemanticAnonymousCondition", "SemanticAuthenticatedCondition", "SemanticRoleCondition", "SemanticClaimCondition",
        "SemanticLogicalPolicyCondition", "SemanticNotPolicyCondition", "SemanticOpaquePolicyCondition", "SemanticPolicyReference", "SemanticLogicalAuthorization",
        "SemanticProjectionValueKey", "SemanticProjectionPartsKey", "SemanticProjectionCompositeKey", "SemanticProjectionLiteral",
        "SemanticProjectionEventProperty", "SemanticProjectionEventSourceIdentity", "SemanticProjectionEventContextValue",
        "SemanticScalarCommandResponse", "SemanticRecordCommandResponse", "SemanticScalarSpecificationResponse", "SemanticRecordSpecificationResponse"
    };

    static readonly HashSet<string> _contextTypes = new(["runtime", "shape", "model"], StringComparer.Ordinal);
    static readonly HashSet<string> _sources = new(StringComparer.Ordinal)
    {
        "context-contract", "derived", "command", "read-model", "current-event", "event-source-id", "model-property", "concept-value",
        "validated-property", "validated-artifact", "authorized-operation", "command-identifier", "query-key"
    };

    internal static bool Known(object node) => KnownType(node.GetType()) && KnownValue(node);

    static bool KnownType(Type type) => !IsPolymorphic(type) || _variants.Contains(type.Name);

    static bool IsPolymorphic(Type type) => type.BaseType?.IsAbstract == true && type.BaseType.Namespace == typeof(SemanticApplication).Namespace;

    static bool KnownValue(object node) => node switch
    {
        Enum value => SemanticDependencyDiscriminators.Supports(value),
        SemanticResolvedExpression expression => KnownExpression(expression),
        SemanticExpression expression => SemanticDependencyDiscriminators.Supports(expression.Kind),
        SemanticTypeReference reference => KnownReference(reference),
        _ => KnownContext(node)
    };

    static bool KnownContext(object node) => node switch
    {
        SemanticContextType context => _contextTypes.Contains(context.Kind),
        SemanticContextSource source => _sources.Contains(source.Kind),
        _ => true
    };

    static bool KnownExpression(SemanticResolvedExpression expression) => SemanticDependencyDiscriminators.Supports(expression.Root) && SemanticDependencyDiscriminators.Supports(expression.Source);

    static bool KnownReference(SemanticTypeReference reference) => SemanticDependencyDiscriminators.Supports(reference.Kind) && HasTarget(reference);

    static bool HasTarget(SemanticTypeReference reference) => reference.Kind == SemanticTypeReferenceKind.Primitive
        ? reference.Primitive != SemanticPrimitiveType.Unknown
        : reference.Target.IsSet;
}
