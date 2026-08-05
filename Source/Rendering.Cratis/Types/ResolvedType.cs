// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Types;

/// <summary>
/// The kind of a <see cref="ResolvedType"/>.
/// </summary>
public enum ResolvedTypeKind
{
    /// <summary>A built-in Screenplay primitive (Uuid, String, Int, Decimal, Bool, Date, DateTime).</summary>
    Primitive,

    /// <summary>A declared <c>concept</c> rendered as <c>ConceptAs&lt;T&gt;</c> or <c>EventSourceId&lt;T&gt;</c>.</summary>
    Concept,

    /// <summary>A declared <c>concept</c> rendered as a C# <see langword="enum"/>.</summary>
    Enum,

    /// <summary>A declared composite <c>type</c>.</summary>
    Composite,

    /// <summary>A referenced name that could not be resolved to a primitive, concept or type.</summary>
    Unresolved,
}

/// <summary>
/// Represents a Screenplay type reference resolved to its generated C# shape.
/// </summary>
/// <param name="ClrTypeName">The C# type name, without collection/optional decoration.</param>
/// <param name="IsCollection">Whether the type is a collection.</param>
/// <param name="IsOptional">Whether the type is optional (nullable).</param>
/// <param name="Kind">The <see cref="ResolvedTypeKind"/> of the resolved type.</param>
public sealed record ResolvedType(string ClrTypeName, bool IsCollection, bool IsOptional, ResolvedTypeKind Kind)
{
    /// <summary>
    /// Renders the full C# type syntax, including collection and optional decoration.
    /// </summary>
    /// <returns>The C# type syntax.</returns>
    public string ToTypeSyntax()
    {
        var type = IsCollection ? $"IReadOnlyList<{ClrTypeName}>" : ClrTypeName;
        return IsOptional ? $"{type}?" : type;
    }
}
