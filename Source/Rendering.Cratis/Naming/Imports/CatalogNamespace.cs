// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents a namespace that exists in the references of a generated project.
/// </summary>
/// <param name="Projects">The projects in which the namespace exists.</param>
/// <param name="Name">The full namespace name.</param>
internal sealed record CatalogNamespace(GeneratedProjects Projects, string Name);
