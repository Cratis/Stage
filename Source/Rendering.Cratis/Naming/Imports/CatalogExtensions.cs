// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents the extension members a namespace brings into scope when it is imported.
/// </summary>
/// <param name="Projects">The projects that see the extensions.</param>
/// <param name="Namespace">The namespace declaring them.</param>
/// <param name="Names">The member names, sorted ordinally.</param>
internal sealed record CatalogExtensions(GeneratedProjects Projects, string Namespace, ImmutableArray<string> Names);
