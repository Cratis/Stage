// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Types;

/// <summary>
/// Resolves a Screenplay <see cref="TypeRefSyntax"/> to its generated C# shape.
/// </summary>
public static class TypeResolver
{
    static readonly Dictionary<string, string> _primitives = new(StringComparer.Ordinal)
    {
        ["Uuid"] = "Guid",
        ["String"] = "string",
        ["Int"] = "int",
        ["Decimal"] = "decimal",
        ["Bool"] = "bool",
        ["Date"] = "DateOnly",
        ["DateTime"] = "DateTimeOffset",
    };

    /// <summary>
    /// Resolves a type reference against an <see cref="ApplicationSet"/>.
    /// </summary>
    /// <param name="type">The type reference to resolve.</param>
    /// <param name="applicationSet">The <see cref="ApplicationSet"/> to resolve concepts and composite types against.</param>
    /// <returns>The <see cref="ResolvedType"/>.</returns>
    public static ResolvedType Resolve(TypeRefSyntax type, ApplicationSet applicationSet)
    {
        if (_primitives.TryGetValue(type.Name, out var clrType))
        {
            return new ResolvedType(clrType, type.IsCollection, type.IsOptional, ResolvedTypeKind.Primitive);
        }

        if (applicationSet.Concepts.TryGetValue(type.Name, out var concept))
        {
            var kind = concept.IsEnum ? ResolvedTypeKind.Enum : ResolvedTypeKind.Concept;
            return new ResolvedType(Identifiers.ToPascalCase(concept.Name), type.IsCollection, type.IsOptional, kind);
        }

        if (applicationSet.Types.TryGetValue(type.Name, out var composite))
        {
            return new ResolvedType(Identifiers.ToPascalCase(composite.Name), type.IsCollection, type.IsOptional, ResolvedTypeKind.Composite);
        }

        return new ResolvedType("object", type.IsCollection, type.IsOptional, ResolvedTypeKind.Unresolved, type.Name);
    }

    /// <summary>
    /// Describes an unresolved type for a rendering diagnostic. The renderer still emits an untyped placeholder so
    /// the generated file compiles — this is what says the emitted type is a placeholder, not the authored one.
    /// </summary>
    /// <param name="type">The <see cref="ResolvedType"/> to describe.</param>
    /// <param name="usage">Where the type was used, for the diagnostic text.</param>
    /// <returns>The diagnostic, or <see langword="null"/> when the type resolved.</returns>
    public static string? DescribeIfUnresolved(ResolvedType type, string usage) =>
        type.Kind == ResolvedTypeKind.Unresolved
            ? $"'{type.SourceName}' ({usage}) is not declared as a concept or type — rendered as 'object'."
            : null;
}
