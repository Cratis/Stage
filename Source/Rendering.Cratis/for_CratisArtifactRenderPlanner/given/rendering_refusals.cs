// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

static class rendering_refusals
{
    internal static void Partial(ArtifactRenderPlan plan)
    {
        Assert.Equal(ArtifactRenderOutcome.Partial, plan.Outcome);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md");
        var gaps = plan.ImplementationGaps.Where(gap => gap.Kind == ArtifactImplementationGapKind.Rendering).ToArray();
        Assert.NotEmpty(gaps);
        foreach (var gap in gaps)
        {
            Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Artifact.ToString() == gap.Owner &&
                diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning && gap.Hints.Contains(diagnostic.Message));
        }
    }

    internal static ArtifactRenderDiagnostic Warning(ArtifactRenderPlan plan, string code, SemanticId owner, string? message = null)
    {
        Partial(plan);
        var diagnostic = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == code && diagnostic.Artifact == owner &&
            (message is null || string.Equals(diagnostic.Message, message, StringComparison.Ordinal))));
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains(plan.ImplementationGaps, gap => gap.Kind == ArtifactImplementationGapKind.Rendering && gap.Owner == owner.ToString() && gap.Hints.Contains(diagnostic.Message));
        return diagnostic;
    }

    internal static ArtifactRenderDiagnostic Original(ArtifactRenderPlan plan, string code)
    {
        var original = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == code &&
            !diagnostic.Message.StartsWith("Generated artifact '", StringComparison.Ordinal)));
        return Warning(plan, code, original.Artifact, original.Message);
    }

    internal static void OmittedParts(ArtifactRenderPlan plan)
    {
        Partial(plan);
        var owners = plan.ImplementationGaps
            .Where(gap => gap.Kind == ArtifactImplementationGapKind.Rendering)
            .SelectMany(gap => plan.Diagnostics.Where(diagnostic =>
                diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning &&
                string.Equals(diagnostic.Artifact.ToString(), gap.Owner, StringComparison.Ordinal) &&
                gap.Hints.Contains(diagnostic.Message)))
            .Select(diagnostic => diagnostic.Artifact)
            .Distinct()
            .ToArray();
        Assert.NotEmpty(owners);
        foreach (var owner in owners)
        {
            Omitted(plan, owner);
        }
#if DEBUG
        Compiles(plan);
#endif
    }

    internal static void Omitted(ArtifactRenderPlan plan, SemanticId owner)
    {
        Partial(plan);
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath != "STAGE-NOT-RUNNABLE.md" && artifact.Sources.Contains(owner));
    }

    internal static void Rejected(ArtifactRenderPlan plan, string code)
    {
        Partial(plan);
        var originals = plan.Diagnostics.Where(diagnostic => diagnostic.Code == code &&
            !diagnostic.Message.StartsWith("Generated artifact '", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(originals);
        foreach (var original in originals)
        {
            Warning(plan, code, original.Artifact, original.Message);
            Omitted(plan, original.Artifact);
        }
#if DEBUG
        Compiles(plan);
#endif
    }

    internal static void Retained(ArtifactRenderPlan plan, SemanticId owner) =>
        Assert.Contains(plan.Artifacts, artifact => artifact.Sources.Contains(owner));

#if DEBUG
    // Translation specifications use client SDK types that the test host also loads from the kernel, so compile them
    // with the client-only reference set, as the translation specification specs do.
    internal static void Compiles(ArtifactRenderPlan plan) => Assert.NotNull(global::Cratis.Stage.Rendering.Cratis.for_CratisRenderer.RenderedOutput.Load(
        opaque_policy_model.Files(plan),
        plan.Artifacts.Any(artifact => artifact.RelativePath.StartsWith("Integration/Translation/", StringComparison.Ordinal))));
#endif
}
