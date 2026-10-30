// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Selects root-only scopes whose complete meaning can be expressed with Chronicle attributes.
/// </summary>
internal static class ModelBoundProjectionEligibility
{
    internal static bool CanRender(SemanticProjection projection, SemanticReadModel model, SemanticApplicationContext context) =>
        ProjectionConventionInput.TryRead(context.Request.Profile, context.Application.Name, out var enabled) && enabled &&
        projection.Scope is { } scope && !SemanticVersionFeatures.HasCompositeKey(model) &&
        scope.Joins.IsEmpty && scope.JoinRemovals.IsEmpty && scope.Nested.IsEmpty && scope.Children.IsEmpty && scope.Every is null &&
        scope.From.All(from => from.ParentKey is null && Key(from.Key) && from.Mappings.All(mapping => Mapping(mapping, model, context))) &&
        scope.Removals.All(removal => removal.ParentKey is null && Key(removal.Key));

    // Payload keys stay fluent until their identity and removal semantics are proven equivalent.
    static bool Key(SemanticProjectionKey key) => key is
        SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity };

    static bool Mapping(SemanticProjectionMapping mapping, SemanticReadModel model, SemanticApplicationContext context)
    {
        if (mapping.Target.Length != 1)
        {
            return false;
        }

        var target = model.Properties.Single(property => property.Id == mapping.Target[0]);
        return mapping.Operation switch
        {
            SemanticProjectionOperation.Set when mapping.Source is SemanticProjectionLiteral literal => Constant(literal, target.Type, context),
            SemanticProjectionOperation.Set => mapping.Source is SemanticProjectionEventSourceIdentity or SemanticProjectionEventProperty { Path.Length: 1 },
            SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract => SemanticScopedProjectionSupport.IsZeroTotalStart(mapping, target, context) ||
                (mapping.Source is SemanticProjectionEventProperty { Path.Length: 1 } && (mapping.Factor is null or SemanticProjectionEventProperty { Path.Length: 1 })),
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement or SemanticProjectionOperation.Clear => true,
            _ => false
        };
    }

    // Decimal, Guid, date and concept constructors are not legal C# attribute arguments. Do not approximate them.
    static bool Constant(SemanticProjectionLiteral literal, SemanticTypeReference type, SemanticApplicationContext context) =>
        !type.IsCollection && ((type.Kind == SemanticTypeReferenceKind.Concept && !context.Concepts[type.Target].Values.IsEmpty) ||
            (type.Kind == SemanticTypeReferenceKind.Primitive && type.Primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.Boolean)) &&
        literal.Value is SemanticTextValue or SemanticNumberValue or SemanticBooleanValue;
}
