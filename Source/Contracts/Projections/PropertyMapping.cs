// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents a single property mapping — pairing a target property path with the source expression that fills it.
/// </summary>
/// <remarks>
/// The <paramref name="Expression"/> is an expression string compatible with Chronicle's projection engine — either a
/// plain event property name, or one of the well-known tokens (for example <c language="csharp">$eventSourceId</c>, <c language="csharp">$eventContext.occurred</c>,
/// <c language="csharp">$value</c>, <c language="csharp">$add</c>, <c language="csharp">$increment</c>).
/// </remarks>
/// <param name="Property">The target property path on the read model.</param>
/// <param name="Expression">The source expression that produces the value.</param>
/// <param name="Factor">
/// The second operand of an <c language="csharp">$add</c> or <c language="csharp">$subtract</c> that adds or subtracts a product of two event
/// values (<c>add total by unitPrice * quantity</c>), or <see langword="null"/> when the mapping has one operand. Chronicle's own projection
/// engine takes a single event property, so a definition carrying a factor cannot be run by Play.
/// </param>
public record PropertyMapping(
    string Property,
    string Expression,
    string? Factor)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyMapping"/> record for a mapping with one operand.
    /// </summary>
    /// <param name="property">The target property path on the read model.</param>
    /// <param name="expression">The source expression that produces the value.</param>
    public PropertyMapping(string property, string expression)
        : this(property, expression, null)
    {
    }

    /// <summary>
    /// Deconstructs the mapping into its target property and source expression.
    /// </summary>
    /// <param name="property">The target property path on the read model.</param>
    /// <param name="expression">The source expression that produces the value.</param>
    public void Deconstruct(out string property, out string expression)
    {
        property = Property;
        expression = Expression;
    }
}
