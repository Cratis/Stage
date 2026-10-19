// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Renders the member an event carries for a product of two event values a projection adds or subtracts.
/// </summary>
internal static class SemanticProjectionProductRendering
{
    /// <summary>
    /// Renders the member an event carries for a product.
    /// </summary>
    /// <param name="product">The product.</param>
    /// <param name="types">The type system of the artifact.</param>
    /// <returns>The C# property declaration.</returns>
    public static string Declaration(SemanticProjectionProduct product, SemanticTypeSystem types)
    {
        var type = types.Primitive(product.Primitive);
        var left = Operand(product.Left);
        var right = Operand(product.Right);
        var expression = product.Primitive == SemanticPrimitiveType.DecimalNumber
            ? $"(decimal){left} * (decimal){right}"
            : $"checked({left} * {right})";
        return $"public {type} {product.Name} {{ get; init; }} = {expression};";
    }

    static string Operand(SemanticProperty property)
    {
        var name = GeneratedPascalCase.From(property.Name);
        return property.Type.Kind == SemanticTypeReferenceKind.Concept ? $"{name}.Value" : name;
    }
}
