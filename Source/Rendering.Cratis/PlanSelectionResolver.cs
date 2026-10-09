// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

internal static class PlanSelectionResolver
{
    internal static (ImmutableArray<ArtifactRenderScope> Scopes, ImmutableArray<CratisPlanDiagnostic> Diagnostics) Resolve(
        SemanticApplication application, PlanSelection selection)
    {
        if (selection.Entries.IsDefaultOrEmpty)
        {
            return ([], [CratisPlanResult.Error("STAGE-PLAN-010", "Select at least one module, feature, or slice.")]);
        }
        var scopes = new List<ArtifactRenderScope>();
        var diagnostics = new List<CratisPlanDiagnostic>();
        foreach (var entry in selection.Entries)
        {
            var resolved = ResolveEntry(application, entry);
            if (resolved.Scope is not null) scopes.Add(resolved.Scope);
            if (resolved.Diagnostic is not null) diagnostics.Add(resolved.Diagnostic);
        }

        return ([.. scopes.Distinct().OrderBy(scope => scope.Kind).ThenBy(scope => scope.Artifact.ToString(), StringComparer.Ordinal)], [.. diagnostics.Distinct()]);
    }

    static (ArtifactRenderScope? Scope, CratisPlanDiagnostic? Diagnostic) ResolveEntry(SemanticApplication application, PlanSelectionEntry entry)
    {
        var path = entry.Path.IsDefault ? [] : entry.Path;
        var moduleName = path.IsEmpty ? string.Empty : path[0];
        var module = application.Modules.SingleOrDefault(module => module.Name == moduleName);
        if (module is null) return (null, Unknown("STAGE-PLAN-011", moduleName, application.Modules.Select(module => module.Name)));
        if (entry.Kind == PlanSelectionKind.Module && path.Length == 1) return (new(ArtifactRenderScopeKind.Module, module.Id), null);
        var lastFeature = entry.Kind == PlanSelectionKind.Slice ? path.Length - 2 : path.Length - 1;
        var features = module.Features;
        SemanticFeature? feature = null;
        for (var index = 1; index <= lastFeature; index++)
        {
            feature = features.SingleOrDefault(candidate => candidate.Name == path[index]);
            if (feature is null) return (null, Unknown("STAGE-PLAN-012", path[index], features.Select(candidate => candidate.Name)));
            features = feature.Features;
        }
        if (feature is null || entry.Kind is not PlanSelectionKind.Feature and not PlanSelectionKind.Slice)
        {
            return (null, Unknown("STAGE-PLAN-012", string.Empty, features.Select(candidate => candidate.Name)));
        }
        if (entry.Kind == PlanSelectionKind.Feature) return (new(ArtifactRenderScopeKind.Feature, feature.Id), null);
        var slice = feature.Slices.SingleOrDefault(candidate => candidate.Name == path[^1]);

        return slice is null
            ? (null, Unknown("STAGE-PLAN-013", path[^1], feature.Slices.Select(candidate => candidate.Name)))
            : (new(ArtifactRenderScopeKind.Slice, slice.Id), null);
    }

    static CratisPlanDiagnostic Unknown(string code, string segment, IEnumerable<string> candidates) =>
        CratisPlanResult.Error(code, $"Unknown name '{segment}'. Known siblings: {string.Join(", ", candidates.Order(StringComparer.Ordinal).Select(name => $"'{name}'"))}.");
}
