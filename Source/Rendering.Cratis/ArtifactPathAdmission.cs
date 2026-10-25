// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

static class ArtifactPathAdmission
{
    internal static List<PlannedArtifact> Admit(IEnumerable<PlannedArtifact> artifacts, SemanticId application, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var admitted = new List<PlannedArtifact>();
        foreach (var group in artifacts.GroupBy(artifact => artifact.RelativePath, StringComparer.OrdinalIgnoreCase))
        {
            var candidates = group.ToArray();
            if (candidates.Length > 1)
            {
                Warn(
                    candidates,
                    application,
                    diagnostics,
                    "STAGE-CRATIS-004",
                    $"Generated artifact path '{group.Key}' collides with another scaffold or semantic artifact.");
            }
            else if (group.Key.StartsWith("Customizations/", StringComparison.OrdinalIgnoreCase))
            {
                Warn(
                    candidates,
                    application,
                    diagnostics,
                    "STAGE-CRATIS-005",
                    "The top-level Customizations directory is reserved for user-owned files. Rename the modeled module or feature that would render into it.");
            }
            else
            {
                admitted.Add(candidates[0]);
            }
        }
        return admitted;
    }

    static void Warn(IEnumerable<PlannedArtifact> artifacts, SemanticId application, List<ArtifactRenderDiagnostic> diagnostics, string code, string message)
    {
        foreach (var owner in artifacts.SelectMany(artifact => artifact.Sources).DefaultIfEmpty(application).Distinct())
        {
            diagnostics.Add(new(code, ArtifactRenderDiagnosticSeverity.Warning, message, owner));
        }
    }
}
