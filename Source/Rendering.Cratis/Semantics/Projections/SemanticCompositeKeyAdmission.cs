// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Identifies the composite read-model keys (ESM v27) the renderer lowers onto Chronicle's composite key support.
/// </summary>
/// <remarks>
/// Chronicle identifies an instance with one object-valued key, so a read model with several key parts renders a key
/// record, an Id property of that record and the declared properties, which keep their own values for queries and specifications.
/// Decimal key parts are refused until Chronicle keys 1.0 and 1.00 as one instance (Chronicle#5427, Stage#847).
/// </remarks>
internal static class SemanticCompositeKeyAdmission
{
    /// <summary>
    /// Gets the name of the generated key property that Chronicle uses as the instance identity.
    /// </summary>
    public const string KeyPropertyName = "Id";

    /// <summary>
    /// Gets the generated C# name of the key record of a composite-keyed read model.
    /// </summary>
    /// <param name="readModel">The read model.</param>
    /// <returns>The key record name.</returns>
    public static string KeyTypeName(SemanticReadModel readModel) => $"{Identifiers.ToPascalCase(readModel.Name)}Key";

    /// <summary>
    /// Returns the first reason a composite-keyed read model cannot be rendered.
    /// </summary>
    /// <param name="context">The semantic application.</param>
    /// <param name="readModel">The read model with several key parts.</param>
    /// <param name="projection">The projection that builds the read model, if any.</param>
    /// <param name="slice">The slice declaring the read model.</param>
    /// <returns>The reason, or null when it can be rendered.</returns>
    public static string? Rejection(SemanticApplicationContext context, SemanticReadModel readModel, SemanticProjection? projection, SemanticSlice slice)
    {
        var identifiers = readModel.Properties.Where(property => property.IsIdentifier).ToArray();
        if (projection?.Scope is not { } scope || !projection.Transitions.IsEmpty)
        {
            return "only a scoped projection builds a composite-keyed read model; reducers and flat projections cannot.";
        }

        if (readModel.Properties.Any(property => string.Equals(Identifiers.ToPascalCase(property.Name), KeyPropertyName, StringComparison.OrdinalIgnoreCase)))
        {
            return $"a property named '{KeyPropertyName}' collides with the generated key property.";
        }

        if (identifiers.Any(part => part.Type.IsOptional || part.Type.IsCollection || part.Type.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept) ||
            SemanticValidationRendering.UnderlyingPrimitive(part.Type, context) == SemanticPrimitiveType.DateTime))
        {
            return "key parts must be required, single, non-date primitives or concepts.";
        }

        // Lift with Cratis/Stage#847 (needs Cratis/Chronicle#5427): remove this block and flip when_refusing_decimal_composite_key_parts.
        var decimalParts = identifiers.Where(part => SemanticValidationRendering.UnderlyingPrimitive(part.Type, context) == SemanticPrimitiveType.DecimalNumber).ToArray();
        if (decimalParts.Length > 0)
        {
            return $"decimal key parts are refused until Cratis/Stage#847 lands: Chronicle keeps a decimal key part's scale, so a replay can split one instance into two when the same value arrives as 1.0 and 1.00 (Cratis/Chronicle#5427). Decimal parts: {string.Join(", ", decimalParts.Select(part => $"'{part.Name}'"))}.";
        }

        if (KeyTypeNameCollides(context, readModel))
        {
            return $"the generated key type '{KeyTypeName(readModel)}' collides with another declaration.";
        }

        if (scope.Every is not null || !scope.Joins.IsEmpty || !scope.JoinRemovals.IsEmpty || !scope.Children.IsEmpty || !scope.Nested.IsEmpty)
        {
            return "composite-keyed projections render only from and remove-with blocks.";
        }

        if (scope.From.Any(from => !KeyCoversParts(from.Key, from.ParentKey, identifiers)) || scope.Removals.Any(removal => !KeyCoversParts(removal.Key, removal.ParentKey, identifiers)))
        {
            return "every from and remove-with block needs a parts key that sets each key part once from an event property.";
        }

        if (scope.From.Any(from => from.Mappings.Any(mapping => mapping.Target.Length > 0 && identifiers.Any(part => part.Id == mapping.Target[0]))))
        {
            return "mappings cannot also set a key part.";
        }

        return slice.Queries.Any(query => query.ReadModel == readModel.Id && query.Delivery == SemanticQueryDelivery.Live && SemanticAnonymousAccess.RequiresCaller(query.Authorization, context.Application.Policies))
            ? "protected live queries over composite keys are not rendered."
            : null;
    }

    /// <summary>
    /// Checks that a projection key sets every key part exactly once from an event property.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="parentKey">The parent key, which a root block does not have.</param>
    /// <param name="identifiers">The read-model key parts.</param>
    /// <returns>Whether the key is a complete parts key.</returns>
    internal static bool KeyCoversParts(SemanticProjectionKey key, SemanticProjectionKey? parentKey, IReadOnlyCollection<SemanticProperty> identifiers) =>
        parentKey is null && key is SemanticProjectionPartsKey parts &&
        parts.Parts.Length == identifiers.Count &&
        parts.Parts.Select(part => part.Property).Distinct().Count() == parts.Parts.Length &&
        parts.Parts.All(part => identifiers.Any(identifier => identifier.Id == part.Property) &&
            part.Value is SemanticProjectionEventProperty { Path.IsEmpty: false });

    static bool KeyTypeNameCollides(SemanticApplicationContext context, SemanticReadModel readModel)
    {
        var name = KeyTypeName(readModel);
        return context.ReadModels.Values.Select(candidate => Identifiers.ToPascalCase(candidate.Name))
            .Concat(context.Events.Values.Select(@event => Identifiers.ToPascalCase(@event.Name)))
            .Concat(context.Concepts.Values.Select(concept => Identifiers.ToPascalCase(concept.Name)))
            .Concat(context.Types.Values.Select(type => Identifiers.ToPascalCase(type.Name)))
            .Any(existing => string.Equals(existing, name, StringComparison.Ordinal));
    }
}
