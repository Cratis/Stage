// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents a shared framework the generated projects compile against. Only the target framework is recorded:
/// the patch of the reference pack follows the installed SDK, while its public types do not change between patches.
/// </summary>
/// <param name="Projects">The projects that reference the framework.</param>
/// <param name="Name">The framework name, such as <c language="csharp">Microsoft.AspNetCore.App</c>.</param>
/// <param name="TargetFramework">The target framework moniker.</param>
internal sealed record CatalogFramework(GeneratedProjects Projects, string Name, string TargetFramework);
