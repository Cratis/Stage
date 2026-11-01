// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Rendering;

/// <summary>
/// Defines the kinds of planner component whose versions are recorded as input provenance. Prerelease: the set can grow.
/// </summary>
public enum ArtifactRenderComponentKind
{
    /// <summary>An unknown kind. Unknown values are never admitted.</summary>
    Unknown = 0,

    /// <summary>A target kit.</summary>
    Kit,

    /// <summary>A kit overlay.</summary>
    Overlay,

    /// <summary>A language pack bound to the kit.</summary>
    LanguagePack,

    /// <summary>The template profile that interpreted the kit.</summary>
    TemplateProfile,

    /// <summary>The template helper catalogue.</summary>
    HelperCatalogue,

    /// <summary>The template engine.</summary>
    TemplateEngine
}
