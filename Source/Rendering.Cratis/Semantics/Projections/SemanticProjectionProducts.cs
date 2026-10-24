// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// A product of two event values that a projection adds to or subtracts from a total (ESM v29, <c language="csharp">add total by unitPrice * quantity</c>),
/// lowered to a property the event carries itself.
/// </summary>
/// <remarks>
/// Chronicle's <c language="csharp">Add</c> and <c language="csharp">Subtract</c> take one event property, and it reads that property from the stored event. The renderer
/// therefore gives the event a computed, serialized member (<c language="csharp">UnitPriceTimesQuantity</c>) that is filled when the event is created, and
/// the projection adds that member. The product is taken exactly: in decimals when either factor is a decimal number, in whole numbers
/// (checked) when both are.
/// </remarks>
/// <param name="Name">The name of the member on the event.</param>
/// <param name="Primitive">The number type of the product.</param>
/// <param name="Left">The factor the mapping adds or subtracts.</param>
/// <param name="Right">The second factor.</param>
internal sealed record SemanticProjectionProduct(string Name, SemanticPrimitiveType Primitive, SemanticProperty Left, SemanticProperty Right);

/// <summary>
/// Finds, admits and renders the products of two event values in projection arithmetic.
/// </summary>
internal static class SemanticProjectionProducts
{
    /// <summary>
    /// Whether a root from block carries a product but its event can never occur in the run, so neither admission nor lowering considers the block.
    /// </summary>
    /// <param name="from">The root from block.</param>
    /// <param name="reachable">The events the run can append.</param>
    /// <returns><see langword="true"/> when the block is skipped.</returns>
    public static bool IsUnreachable(SemanticProjectionFrom from, IReadOnlySet<SemanticId> reachable) =>
        from.Mappings.Any(mapping => mapping.Factor is not null) && !reachable.Contains(from.EventContract);

    /// <summary>
    /// Finds the reason a mapping with a second factor cannot be lowered.
    /// </summary>
    /// <param name="mapping">The mapping.</param>
    /// <param name="inFromBlock">Whether the mapping is in a from block of its scope; a join or every block has no event of its own to carry the member.</param>
    /// <param name="event">The event the block is triggered by.</param>
    /// <param name="targets">The properties of the level the mapping targets.</param>
    /// <param name="context">The semantic application.</param>
    /// <param name="product">The lowering when there is one.</param>
    /// <returns>The reason, or <see langword="null"/> when the mapping lowers.</returns>
    public static string? Rejection(
        SemanticProjectionMapping mapping,
        bool inFromBlock,
        SemanticEventContract @event,
        IReadOnlyList<SemanticProperty> targets,
        SemanticApplicationContext context,
        out SemanticProjectionProduct? product)
    {
        product = null;
        if (!inFromBlock)
        {
            return "only a from block can add or subtract a product; a join or every block has no event of its own to carry it";
        }

        if (mapping.Source is not SemanticProjectionEventProperty { Path.Length: 1 } first || mapping.Factor is not SemanticProjectionEventProperty { Path.Length: 1 } second)
        {
            return "both factors must be properties of the event";
        }

        var left = @event.Properties.SingleOrDefault(property => property.Id == first.Path[0]);
        var right = @event.Properties.SingleOrDefault(property => property.Id == second.Path[0]);
        if (left is null || right is null)
        {
            return "both factors must be properties of the event";
        }

        if (Number(left.Type, context) is not { } leftNumber || Number(right.Type, context) is not { } rightNumber)
        {
            return $"'{left.Name}' and '{right.Name}' must both be required whole or decimal numbers";
        }

        var primitive = leftNumber == SemanticPrimitiveType.DecimalNumber || rightNumber == SemanticPrimitiveType.DecimalNumber
            ? SemanticPrimitiveType.DecimalNumber
            : SemanticPrimitiveType.WholeNumber;
        var name = $"{GeneratedPascalCase.From(left.Name)}Times{GeneratedPascalCase.From(right.Name)}";
        if (@event.Properties.Any(property => string.Equals(GeneratedPascalCase.From(property.Name), name, StringComparison.OrdinalIgnoreCase)))
        {
            return $"the event already has a property named '{name}', which the product needs";
        }

        if (SemanticScopedProjectionSupport.Target(mapping.Target, targets, context) is { } target &&
            (target.Type.IsCollection || Underlying(target.Type, context) != primitive))
        {
            return $"the total '{target.Name}' holds {(Underlying(target.Type, context) == SemanticPrimitiveType.WholeNumber ? "a whole" : "another kind of")} number but the product of '{left.Name}' and '{right.Name}' is {(primitive == SemanticPrimitiveType.DecimalNumber ? "a decimal" : "a whole")} number";
        }

        product = new(name, primitive, left, right);
        return null;
    }

    /// <summary>
    /// Finds the products every projection of the application adds or subtracts from an event.
    /// </summary>
    /// <param name="event">The event.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>The distinct members the event carries, in model order.</returns>
    public static IReadOnlyList<SemanticProjectionProduct> For(SemanticEventContract @event, SemanticApplicationContext context)
    {
        var products = new Dictionary<string, SemanticProjectionProduct>(StringComparer.Ordinal);
        foreach (var projection in context.Projections.Values.Where(_ => _.Scope is not null && context.ReadModels.ContainsKey(_.ReadModel)))
        {
            foreach (var found in Walk(projection.Scope!, context.ReadModels[projection.ReadModel].Properties, context))
            {
                if (found.Event is { } owner && owner.Id == @event.Id && found.Product is { } product)
                {
                    products.TryAdd(product.Name, product);
                }
            }
        }

        return [.. products.Values];
    }

    /// <summary>
    /// Finds the product of a mapping in a from block that renders.
    /// </summary>
    /// <param name="mapping">The mapping.</param>
    /// <param name="event">The event of the from block.</param>
    /// <param name="targets">The properties of the level the mapping targets.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>The product, or <see langword="null"/> when the mapping has no second factor or does not lower.</returns>
    public static SemanticProjectionProduct? Of(
        SemanticProjectionMapping mapping,
        SemanticEventContract @event,
        IReadOnlyList<SemanticProperty> targets,
        SemanticApplicationContext context)
    {
        if (mapping.Factor is null)
        {
            return null;
        }

        Rejection(mapping, true, @event, targets, context, out var product);
        return product;
    }

    /// <summary>
    /// Visits every mapping with a second factor of a scope and what becomes of it.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="targets">The properties of the scope's level.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>One entry per mapping with a second factor.</returns>
    public static IEnumerable<ProductMapping> Walk(SemanticProjectionScope scope, IReadOnlyList<SemanticProperty> targets, SemanticApplicationContext context)
    {
        foreach (var from in scope.From.Where(_ => context.Events.ContainsKey(_.EventContract)))
        {
            var @event = context.Events[from.EventContract];
            foreach (var mapping in from.Mappings.Where(_ => _.Factor is not null))
            {
                yield return new(mapping, @event, Rejection(mapping, true, @event, targets, context, out var product), product);
            }
        }

        foreach (var join in scope.Joins.Where(_ => context.Events.ContainsKey(_.EventContract)))
        {
            var @event = context.Events[join.EventContract];
            foreach (var mapping in join.Mappings.Where(_ => _.Factor is not null))
            {
                yield return new(mapping, @event, Rejection(mapping, false, @event, targets, context, out var product), product);
            }
        }

        foreach (var mapping in scope.Every?.Mappings.Where(_ => _.Factor is not null) ?? [])
        {
            yield return new(mapping, null, Rejection(mapping, false, null!, targets, context, out var product), product);
        }

        foreach (var nested in scope.Nested)
        {
            if (targets.SingleOrDefault(property => property.Id == nested.Property) is { } property && context.Types.TryGetValue(property.Type.Target, out var type))
            {
                foreach (var found in Walk(nested.Scope, type.Properties, context))
                {
                    yield return found;
                }
            }
        }

        foreach (var children in scope.Children)
        {
            if (targets.SingleOrDefault(property => property.Id == children.Property) is { } property && context.Types.TryGetValue(property.Type.Target, out var type))
            {
                foreach (var found in Walk(children.Scope, type.Properties, context))
                {
                    yield return found;
                }
            }
        }
    }

    static SemanticPrimitiveType? Number(SemanticTypeReference type, SemanticApplicationContext context)
    {
        if (type.IsOptional || type.IsCollection || type.Kind is not (SemanticTypeReferenceKind.Primitive or SemanticTypeReferenceKind.Concept))
        {
            return null;
        }

        var primitive = Underlying(type, context);
        return primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber ? primitive : null;
    }

    static SemanticPrimitiveType Underlying(SemanticTypeReference type, SemanticApplicationContext context) =>
        type.Kind == SemanticTypeReferenceKind.Concept ? context.Concepts[type.Target].Primitive : type.Primitive;
}

/// <summary>
/// One mapping with a second factor, with the event that carries its product and the reason it does not lower.
/// </summary>
/// <param name="Mapping">The mapping.</param>
/// <param name="Event">The event of its block, or <see langword="null"/> in an every block.</param>
/// <param name="Rejection">The reason it does not lower, or <see langword="null"/>.</param>
/// <param name="Product">The lowering when there is one.</param>
internal sealed record ProductMapping(SemanticProjectionMapping Mapping, SemanticEventContract? Event, string? Rejection, SemanticProjectionProduct? Product);
