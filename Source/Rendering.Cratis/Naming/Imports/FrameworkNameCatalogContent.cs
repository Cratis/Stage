// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents everything the framework name catalog records, in the order its text lists it.
/// </summary>
/// <param name="ScaffoldVersion">The scaffold contract version the catalog was generated for.</param>
/// <param name="Pins">The exact versions the scaffold pins, keyed by package id or <c language="csharp">TargetFramework</c>.</param>
/// <param name="Packages">The packages in the restored dependency graphs.</param>
/// <param name="Frameworks">The shared frameworks.</param>
/// <param name="Usings">The global using directives.</param>
/// <param name="Scopes">The namespace prefixes whose types are recorded in full, besides every imported namespace.</param>
/// <param name="Namespaces">Every namespace in the references.</param>
/// <param name="Types">Every public type in the recorded namespaces.</param>
/// <param name="Extensions">The extension members of each recorded namespace.</param>
/// <param name="BaseTypes">The classes in the recorded namespaces a generated type may derive from.</param>
internal sealed record FrameworkNameCatalogContent(
    string ScaffoldVersion,
    ImmutableSortedDictionary<string, string> Pins,
    ImmutableArray<CatalogPackage> Packages,
    ImmutableArray<CatalogFramework> Frameworks,
    ImmutableArray<CatalogUsing> Usings,
    ImmutableArray<string> Scopes,
    ImmutableArray<CatalogNamespace> Namespaces,
    ImmutableArray<CatalogedType> Types,
    ImmutableArray<CatalogExtensions> Extensions,
    ImmutableArray<CatalogBaseType> BaseTypes);
