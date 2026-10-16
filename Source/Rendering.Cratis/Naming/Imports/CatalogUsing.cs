// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents a global using directive every file of a generated project compiles with.
/// </summary>
/// <param name="Projects">The projects the directive applies to.</param>
/// <param name="Kind">The kind of directive.</param>
/// <param name="Target">The imported namespace, or the type of a static or alias directive.</param>
/// <param name="Alias">The alias of an alias directive, empty otherwise.</param>
/// <param name="Source">Where the directive comes from: <c language="csharp">sdk</c> for an implicit using, otherwise the contributing package.</param>
internal sealed record CatalogUsing(GeneratedProjects Projects, CatalogUsingKind Kind, string Target, string Alias, string Source);
