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
        // Specification refusals remain distinct from rendering gaps, including in mixed plans.
        var errors = diagnostics.Where(diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error &&
            (string.Equals(diagnostic.Code, "STAGE-ESM-011", StringComparison.Ordinal) ||
                string.Equals(diagnostic.Code, "STAGE-ESM-024", StringComparison.Ordinal) ||
                string.Equals(diagnostic.Code, SemanticSpecificationAdmission.SourcelessGivenCode, StringComparison.Ordinal)) &&
            context.Specifications.ContainsKey(diagnostic.Artifact)).ToArray();

        return [.. errors.GroupBy(error => error.Artifact).OrderBy(group => group.Key.ToString(), StringComparer.Ordinal)
            .Select(group => new ArtifactRenderRefusal(group.Key, "specification", [.. group.Select(error => error.Code).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]))];
    }
}
