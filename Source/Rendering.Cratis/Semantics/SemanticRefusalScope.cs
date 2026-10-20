// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

static class SemanticRefusalScope
{
    public static ImmutableArray<ArtifactRenderRefusal> Classify(
        SemanticApplicationContext context,
        IEnumerable<ArtifactRenderDiagnostic> diagnostics)
    {
        var errors = diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error).ToArray();

        // Only specification execution gaps are safe to omit: neither code permits dropping application contracts.
        if (errors.Length == 0 || errors.Any(error =>
            error.Code is not ("STAGE-ESM-011" or "STAGE-ESM-024" or SemanticSpecificationAdmission.SourcelessGivenCode) || !context.Specifications.ContainsKey(error.Artifact)))
        {
            return [];
        }

        return [.. errors.GroupBy(error => error.Artifact).OrderBy(group => group.Key.ToString(), StringComparer.Ordinal)
            .Select(group => new ArtifactRenderRefusal(group.Key, "specification", [.. group.Select(error => error.Code).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]))];
    }
}
