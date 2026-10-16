// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents a package in the restored dependency graph of the generated projects.
/// </summary>
/// <param name="Projects">The projects whose restore graph contains the package.</param>
/// <param name="Id">The package id.</param>
/// <param name="Version">The resolved package version.</param>
internal sealed record CatalogPackage(GeneratedProjects Projects, string Id, string Version);
