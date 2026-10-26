// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_non_csharp_body_is_planned_strictly
{
    [Fact]
    public async Task should_attach_the_same_evidence_and_promote_only_in_strict_mode()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.Source.Replace("```csharp", "```java", StringComparison.Ordinal));
        var model = loaded.Model;
        ArtifactRenderPlan Plan(bool strict) => new CratisArtifactRenderPlanner().Plan(new ArtifactRenderRequest(
            model,
            SemanticExecutionPlan.Compile(model).Plan,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects") { Strict = strict }),
            new(ArtifactRenderScopeKind.Application, model.Application.Id))
        {
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors
        });

        var normal = Plan(false);
        var strict = Plan(true);

        var normalEvidence = Assert.Single(normal.Degradations);
        Assert.Equal(ArtifactRenderDegradationCodes.InlineMismatch, normalEvidence.Diagnostic.Code);
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Warning, normalEvidence.Diagnostic.Severity);
        Assert.Equal(ImplementationLanguage.Remedy, normalEvidence.Remedy);
        Assert.False(normal.Strict);
        Assert.Equal(ArtifactRenderOutcome.Degraded, normal.Outcome);

        Assert.True(strict.Strict);
        Assert.Contains(strict.Degradations, evidence => evidence.Diagnostic == normalEvidence.Diagnostic);
        Assert.Contains(strict.Diagnostics, diagnostic => diagnostic.Code == ArtifactRenderDegradationCodes.InlineMismatch && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        var restored = strict.WithAcceptance(strict.Degradations, strict: false);
        Assert.Contains(restored.Diagnostics, diagnostic => diagnostic == normalEvidence.Diagnostic);
        Assert.Equal(ArtifactRenderOutcome.Degraded, restored.Outcome);
        Assert.True(restored.Success);
    }
}
#endif
