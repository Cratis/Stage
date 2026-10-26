// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    // Resolves the slices present under the application's other domains. A slice belongs to one domain only, and the
    // plan's own domain is named through UserInterface, so the import of every present proxy is unambiguous.
    static (List<KeyValuePair<string, IEnumerable<string>>>? Domains, CratisPlanDiagnostic? Diagnostic) UserInterfaceDomains(
        LoadedSemanticModel loaded,
        CratisPlanOptions options,
        ArtifactRenderProfile profile)
    {
        if (options.UserInterface is not { } userInterface)
        {
            return (null, CratisPlanResult.Error("STAGE-PLAN-024", "UserInterfaceDomains requires the UserInterface option."));
        }

        if (profile.Inputs.Any(input => input.Name == UserInterfaceDomainsInput.Name))
        {
            return (null, ProfileError("The caller's profile must omit the user interface domains input when the UserInterfaceDomains option is set."));
        }

        DomainPlacementInput.TryNormalize(options.Domain, out var own);
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var slice in userInterface.Entries.IsDefaultOrEmpty ? [] : PlanSelectionResolver.Slices(loaded.Model.Application, PlanSelectionResolver.Resolve(loaded.Model.Application, userInterface).Scopes))
        {
            owners[slice] = own;
        }

        var domains = new List<KeyValuePair<string, IEnumerable<string>>>();
        foreach (var entry in options.UserInterfaceDomains)
        {
            if (entry?.Domain is null || entry.Slices is null ||
                !DomainPlacementInput.TryNormalize(entry.Domain, out var domain) || DomainPlacementInput.IsReserved(domain, profile))
            {
                return (null, CratisPlanResult.Error("STAGE-PLAN-025", $"User interface domain '{entry?.Domain}' must contain valid relative C# name segments, without traversal or empty segments, outside the reserved and scaffold folders."));
            }

            if (domain == own || domains.Exists(named => named.Key == domain))
            {
                return (null, CratisPlanResult.Error("STAGE-PLAN-026", $"User interface domain '{domain}' is the plan's own domain or is named more than once; name the plan's own slices through UserInterface."));
            }

            var resolved = entry.Slices.Entries.IsDefaultOrEmpty ? ([], []) : PlanSelectionResolver.Resolve(loaded.Model.Application, entry.Slices);
            if (!resolved.Diagnostics.IsEmpty)
            {
                var first = resolved.Diagnostics[0];
                return (null, first with { Message = $"User interface domain '{domain}': {first.Message}" });
            }

            var slices = PlanSelectionResolver.Slices(loaded.Model.Application, resolved.Scopes).ToArray();
            foreach (var slice in slices)
            {
                if (!owners.TryAdd(slice, domain))
                {
                    return (null, CratisPlanResult.Error("STAGE-PLAN-027", $"The slice '{slice}' is present under both the '{owners[slice]}' and '{domain}' domains; a slice's generated code lives under one domain."));
                }
            }

            domains.Add(KeyValuePair.Create(domain, (IEnumerable<string>)slices));
        }

        return (domains, null);
    }
}
