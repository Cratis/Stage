// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// Represents one externally visible type read from a reference assembly's metadata.
/// </summary>
/// <param name="Namespace">The namespace, empty for the global namespace.</param>
/// <param name="MetadataName">The metadata name inside the namespace, nested types separated by a plus.</param>
/// <param name="Attributes">The type attributes.</param>
/// <param name="BaseFullName">The full metadata name of the base type, empty when there is none.</param>
/// <param name="Members">The public and protected member names the type declares.</param>
/// <param name="NestedTypes">The public and protected nested type names the type declares.</param>
/// <param name="ExtensionNames">The extension member names the type declares, empty unless it is an extension container.</param>
sealed record MetadataType(
    string Namespace,
    string MetadataName,
    TypeAttributes Attributes,
    string BaseFullName,
    ImmutableArray<string> Members,
    ImmutableArray<string> NestedTypes,
    ImmutableArray<string> ExtensionNames)
{
    /// <summary>
    /// Gets the full metadata name.
    /// </summary>
    public string FullName => CatalogNames.Qualify(Namespace, MetadataName);

    /// <summary>
    /// Gets a value indicating whether the type is an interface.
    /// </summary>
    public bool IsInterface => Attributes.HasFlag(TypeAttributes.Interface);

    /// <summary>
    /// Gets a value indicating whether the type is a static class.
    /// </summary>
    public bool IsStatic => !IsInterface && Attributes.HasFlag(TypeAttributes.Abstract | TypeAttributes.Sealed);

    /// <summary>
    /// Gets a value indicating whether another type can derive from the type. Structs, enums and delegates are sealed, while
    /// the classes they derive from are not, so a generated enum still finds what it inherits.
    /// </summary>
    public bool IsDerivable => !IsInterface && !Attributes.HasFlag(TypeAttributes.Sealed);
}
