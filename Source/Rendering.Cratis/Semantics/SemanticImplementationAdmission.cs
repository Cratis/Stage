// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Verifies the resolved implementation envelope without interpreting implementation code.</summary>
internal static class SemanticImplementationAdmission
{
    internal static ImmutableArray<ArtifactRenderDiagnostic> Verify(ArtifactRenderRequest request)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ArtifactRenderDiagnostic>();
        var requirements = request.ImplementationRequirements;
        var contents = request.ImplementationContents;
        if (requirements.IsDefault || contents is null)
        {
            diagnostics.Add(Error("The implementation attachment envelope is missing.", request.Model.Application.Id));
            return diagnostics.ToImmutable();
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var requirement in requirements)
        {
            if (requirement is null || string.IsNullOrEmpty(requirement.RequirementId) || !seen.Add(requirement.RequirementId))
            {
                diagnostics.Add(Error("The implementation attachment envelope contains a missing or duplicate requirement identity.", request.Model.Application.Id));
                continue;
            }

            if (requirement.AttachmentResolution != SemanticAttachmentResolution.Resolved ||
                !contents.TryGetValue(requirement.RequirementId, out var body) || body is null)
            {
                diagnostics.Add(Error($"Implementation requirement '{requirement.RequirementId}' has no resolved body.", requirement.Source.SemanticId));
                continue;
            }

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
            if (!string.Equals(hash, requirement.ContentHash, StringComparison.Ordinal))
            {
                diagnostics.Add(Error($"Implementation requirement '{requirement.RequirementId}' has a body whose SHA-256 differs from ContentHash.", requirement.Source.SemanticId));
            }
        }

        foreach (var id in contents.Keys.Where(id => !seen.Contains(id)).Order(StringComparer.Ordinal))
        {
            diagnostics.Add(Error($"Implementation body '{id}' has no compiler requirement.", request.Model.Application.Id));
        }

        return diagnostics.ToImmutable();
    }

    static ArtifactRenderDiagnostic Error(string message, SemanticId artifact) =>
        new("STAGE-ESM-020", ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
