// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Decides whether a projection that adds or subtracts a product of two values (ESM v29) can be lowered to a Chronicle reducer.
/// </summary>
/// <remarks>
/// Chronicle's projection engine adds or subtracts exactly one event value, so a product has no projection form. The admitted
/// subset is the one whose reference meaning a reducer reproduces exactly: root-level from and remove-with blocks
/// keyed by the event source, scalar targets, and every required property established by each block that can create the model.
/// </remarks>
internal static class SemanticProductReducerAdmission
{
    /// <summary>
    /// Whether the projection holds a product and lowers to a reducer with the reference meaning.
    /// </summary>
    /// <param name="projection">The semantic projection.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>Whether the projection renders as a product reducer.</returns>
    public static bool Admits(SemanticProjection projection, SemanticApplicationContext context)
    {
        if (FlatProductScope(projection) is not { } scope || !context.ReadModels.TryGetValue(projection.ReadModel, out var readModel))
        {
            return false;
        }

        return SingleIdentity(readModel, context) is { } identifier && ScopeAdmitted(scope, readModel, identifier, context);
    }

    /// <summary>
    /// Whether a type is a whole or decimal number, directly or through a concept.
    /// </summary>
    /// <param name="type">The type reference.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>Whether arithmetic applies to the type.</returns>
    internal static bool IsNumber(SemanticTypeReference type, SemanticApplicationContext context) =>
        !type.IsCollection && NumberPrimitive(type, context) is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber;

    /// <summary>
    /// Gets the primitive a scalar or plain concept type carries.
    /// </summary>
    /// <param name="type">The type reference.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>The primitive, or <see langword="null"/> for any other type.</returns>
    internal static SemanticPrimitiveType? NumberPrimitive(SemanticTypeReference type, SemanticApplicationContext context) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => type.Primitive,
        SemanticTypeReferenceKind.Concept when context.Concepts.TryGetValue(type.Target, out var concept) && concept.Values.IsEmpty &&
            !context.IdentifierConcepts.Contains(concept.Id) => concept.Primitive,
        _ => null
    };

    /// <summary>
    /// Whether the reducer class generated for a projection can declare its `On` methods; C# forbids a member named like its type.
    /// </summary>
    /// <param name="projection">The semantic projection.</param>
    /// <param name="readModel">The target read model.</param>
    /// <returns>Whether the generated reducer type name differs from `On`.</returns>
    internal static bool ReducerTypeNameIsSafe(SemanticProjection projection, SemanticReadModel readModel) =>
        GeneratedPascalCase.From(projection.Name) != "On" || GeneratedPascalCase.From(readModel.Name) == "On";

    static bool ScopeAdmitted(SemanticProjectionScope scope, SemanticReadModel readModel, SemanticProperty identifier, SemanticApplicationContext context) =>
        readModel.Properties.All(property => property.IsIdentifier || IsScalar(property.Type, context)) &&
            HasDistinctContracts(scope) &&
            scope.Removals.All(IsRootEventSourceRemoval) &&
            scope.From.All(from => FromAdmitted(from, readModel, identifier, context));

    static SemanticProjectionScope? FlatProductScope(SemanticProjection projection) =>
        projection.Target == SemanticProjectionTargetKind.ReadModel && projection.Scope is { } scope && HasProduct(scope) && IsFlat(scope) ? scope : null;

    static SemanticProperty? SingleIdentity(SemanticReadModel readModel, SemanticApplicationContext context)
    {
        var identifiers = readModel.Properties.Where(property => property.IsIdentifier).ToArray();
        return identifiers.Length == 1 && IsIdentity(identifiers[0].Type, context) ? identifiers[0] : null;
    }

    static bool HasDistinctContracts(SemanticProjectionScope scope)
    {
        var contracts = scope.From.Select(from => from.EventContract).Concat(scope.Removals.Select(removal => removal.EventContract)).ToArray();
        return contracts.Distinct().Count() == contracts.Length;
    }

    static bool IsRootEventSourceRemoval(SemanticProjectionRemoval removal) => IsEventSourceKey(removal.Key) && removal.ParentKey is null;

    static bool HasProduct(SemanticProjectionScope scope) => scope.From.Any(from => from.Mappings.Any(mapping => mapping.Factor is not null));

    static bool IsFlat(SemanticProjectionScope scope) =>
        scope.Joins.IsEmpty && scope.Children.IsEmpty && scope.Nested.IsEmpty && scope.Every is null && scope.JoinRemovals.IsEmpty;

    static bool IsEventSourceKey(SemanticProjectionKey key) => key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity };

    static bool IsIdentity(SemanticTypeReference type, SemanticApplicationContext context) =>
        !type.IsCollection && !type.IsOptional && type.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => type.Primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text,
            SemanticTypeReferenceKind.Concept => context.Concepts.TryGetValue(type.Target, out var concept) && concept.Values.IsEmpty &&
                concept.Primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text,
            _ => false
        };

    static bool IsScalar(SemanticTypeReference type, SemanticApplicationContext context) =>
        !type.IsCollection && type.Kind is SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept &&
        (type.Kind == SemanticTypeReferenceKind.Primitive || context.Concepts.ContainsKey(type.Target));

    static bool FromAdmitted(SemanticProjectionFrom from, SemanticReadModel readModel, SemanticProperty identifier, SemanticApplicationContext context)
    {
        if (!IsEventSourceKey(from.Key) || from.ParentKey is not null || !context.Events.TryGetValue(from.EventContract, out var @event))
        {
            return false;
        }

        var targets = readModel.Properties.ToDictionary(property => property.Id);
        if (!from.Mappings.All(mapping => mapping.Target.Length == 1 && targets.TryGetValue(mapping.Target[0], out var target) &&
            MappingAdmitted(mapping, target, identifier, @event, context)))
        {
            return false;
        }

        // The reference refuses a created model that lacks a required value; a reducer would store a default instead.
        var established = from.Mappings.Where(mapping => mapping.Operation != SemanticProjectionOperation.Clear).Select(mapping => mapping.Target[0]).ToHashSet();
        return readModel.Properties.All(property => property.IsIdentifier || property.Type.IsOptional || established.Contains(property.Id));
    }

    static bool MappingAdmitted(SemanticProjectionMapping mapping, SemanticProperty target, SemanticProperty identifier, SemanticEventContract @event, SemanticApplicationContext context)
    {
        if (target.IsIdentifier)
        {
            // The identifier always carries the event source; mapping it from anything else would move the instance.
            return mapping is { Operation: SemanticProjectionOperation.Set, Source: SemanticProjectionEventSourceIdentity, Factor: null };
        }

        return mapping.Operation switch
        {
            SemanticProjectionOperation.Set => mapping.Factor is null && SetAdmitted(mapping.Source, target.Type, identifier.Type, @event),
            SemanticProjectionOperation.Clear => mapping.Factor is null && target.Type.IsOptional,
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement => mapping.Factor is null && IsNumber(target.Type, context),
            SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract => IsNumber(target.Type, context) &&
                NumberAdmitted(mapping.Source, @event, context) && (mapping.Factor is null || NumberAdmitted(mapping.Factor, @event, context)),
            _ => false
        };
    }

    static bool SetAdmitted(SemanticProjectionValue? source, SemanticTypeReference target, SemanticTypeReference identifier, SemanticEventContract @event) => source switch
    {
        SemanticProjectionEventProperty property => EventProperty(property, @event) is { } eventProperty &&
            SameType(eventProperty.Type, target) && (target.IsOptional || !eventProperty.Type.IsOptional),
        SemanticProjectionLiteral { Value: SemanticNullValue } => target.IsOptional,
        SemanticProjectionLiteral => true,
        SemanticProjectionEventSourceIdentity => SameType(identifier, target),
        _ => false
    };

    // A null operand fails the reference projection, so only required numeric event values and number literals are admitted.
    static bool NumberAdmitted(SemanticProjectionValue? value, SemanticEventContract @event, SemanticApplicationContext context) => value switch
    {
        SemanticProjectionEventProperty property => EventProperty(property, @event) is { } eventProperty &&
            !eventProperty.Type.IsOptional && IsNumber(eventProperty.Type, context),
        SemanticProjectionLiteral { Value: SemanticNumberValue } => true,
        _ => false
    };

    static SemanticProperty? EventProperty(SemanticProjectionEventProperty property, SemanticEventContract @event) =>
        property.Path.Length == 1 ? @event.Properties.SingleOrDefault(candidate => candidate.Id == property.Path[0]) : null;

    static bool SameType(SemanticTypeReference left, SemanticTypeReference right) =>
        left with { IsOptional = false } == right with { IsOptional = false };
}
