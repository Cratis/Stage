// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

/// <summary>
/// Represents one compile reference of a restored scaffold project.
/// </summary>
/// <param name="Path">The reference assembly path.</param>
/// <param name="Package">The package the assembly comes from, empty for a shared framework.</param>
/// <param name="Framework">The shared framework the assembly belongs to, empty for a package.</param>
sealed record ScaffoldedReference(string Path, string Package, string Framework);
