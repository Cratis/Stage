// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Kits;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>The locked stage-templates/v1 profile.</summary>
public static class TemplateProfile
{
    /// <summary>The profile identity.</summary>
    public const string Id = "stage-templates/v1";

    /// <summary>The maximum UTF-8 template size.</summary>
    public const int MaxTemplateBytes = 65536;

    /// <summary>The maximum pre-scan nesting.</summary>
    public const int MaxDepth = 32;

    /// <summary>Gets the profile contract.</summary>
    public static ContractVersion Contract { get; } = new("templates", 1, 0);

    /// <summary>Parses and lints every declared source without executing it.</summary>
    /// <param name="kit">Validated declarative kit.</param>
    /// <param name="language">Target-owned spelling seam.</param>
    /// <param name="overrides">Section replacement bodies.</param>
    /// <returns>Prepared templates or ordered safety refusals.</returns>
    public static TemplatePrepareResult Prepare(LoadedKit kit, ITemplateLanguage language, TemplateSectionOverrides? overrides = null)
    {
        var prepared = TemplatePreparer.Prepare(kit, overrides);
        return new(prepared.Value is null ? null : new(prepared.Value, language), prepared.Diagnostics);
    }
}
