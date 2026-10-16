// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Splits metadata type names such as <c language="csharp">List`1</c> into a C# name and a generic arity.
/// </summary>
internal static class CatalogNames
{
    /// <summary>
    /// Gets the name of a metadata type name without its generic arity.
    /// </summary>
    /// <param name="metadataName">The metadata name of one type, without nesting.</param>
    /// <returns>The C# name.</returns>
    public static string SimpleName(string metadataName)
    {
        var tick = metadataName.IndexOf('`', StringComparison.Ordinal);
        return tick < 0 ? metadataName : metadataName[..tick];
    }

    /// <summary>
    /// Gets the generic arity of a metadata type name.
    /// </summary>
    /// <param name="metadataName">The metadata name of one type, without nesting.</param>
    /// <returns>The arity, zero for a non-generic type.</returns>
    public static int Arity(string metadataName)
    {
        var tick = metadataName.IndexOf('`', StringComparison.Ordinal);
        return tick < 0 ? 0 : int.Parse(metadataName[(tick + 1)..], CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the metadata name of a C# name with a generic arity.
    /// </summary>
    /// <param name="name">The C# name.</param>
    /// <param name="arity">The generic arity.</param>
    /// <returns>The metadata name.</returns>
    public static string MetadataName(string name, int arity) =>
        arity == 0 ? name : $"{name}`{arity.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Joins a namespace and a name the way C# qualifies them.
    /// </summary>
    /// <param name="namespace">The namespace, empty for the global namespace.</param>
    /// <param name="name">The name inside it.</param>
    /// <returns>The qualified name.</returns>
    public static string Qualify(string @namespace, string name) => @namespace.Length == 0 ? name : $"{@namespace}.{name}";

    /// <summary>
    /// Gets the namespace that contains a namespace, empty when it is a top-level namespace.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The containing namespace.</returns>
    public static string Parent(string @namespace)
    {
        var dot = @namespace.LastIndexOf('.');
        return dot < 0 ? string.Empty : @namespace[..dot];
    }

    /// <summary>
    /// Enumerates a namespace and every namespace that contains it, innermost first, ending with the global namespace.
    /// </summary>
    /// <param name="namespace">The namespace.</param>
    /// <returns>The namespaces, innermost first.</returns>
    public static IEnumerable<string> SelfAndContaining(string @namespace)
    {
        for (var current = @namespace; current.Length > 0; current = Parent(current))
        {
            yield return current;
        }

        yield return string.Empty;
    }
}
