// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Kits;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>The locked stage-templates/v1 profile.</summary>
public static class TemplateProfile
{
    /// <summary>Gets the profile identity.</summary>
    public const string Version = "stage-templates/v1";

    /// <summary>Gets the contracts understood by this profile.</summary>
    public static ImmutableArray<ContractVersion> SupportedContracts { get; } = [new("templates", 1, 0), new("naming", 1, 0), new("operators", 1, 0), new("literals", 1, 0), new("symbols", 1, 0)];

    /// <summary>Validates and parses a loaded kit without executing any source.</summary>
    /// <param name="kit">Validated declarative kit.</param>
    /// <param name="language">Target-owned escaping and safe placeholder seam.</param>
    /// <param name="contractOverrides">Optional additional compatibility constraints.</param>
    /// <returns>Prepared templates or aggregated safety refusals.</returns>
    public static TemplatePrepareResult Prepare(LoadedKit kit, ITemplateLanguage language, ContractOverrides? contractOverrides = null)
    {
        var errors = ImmutableArray.CreateBuilder<TemplateDiagnostic>();
        foreach (var required in kit.Manifest.Requires)
        {
            var supported = SupportedContracts.FirstOrDefault(contract => contract.Name == required.Name);
            if (supported.Name is null || supported.Major != required.Major || supported.Minor < required.Minor ||
                (contractOverrides?.Supported.Any(contract => contract.Name == required.Name && (contract.Major != required.Major || contract.Minor < required.Minor)) == true))
            {
                errors.Add(new(TemplateCodes.MissingContract, "Unsupported helper contract: " + required.Name, string.Empty, 0, 0));
            }
        }

        var prepared = TemplatePreparer.Prepare(kit, contractOverrides);
        errors.AddRange(prepared.Diagnostics);
        return errors.Count > 0 ? new(null, errors.ToImmutable()) : new(new(prepared.Value!, language), []);
    }
}
