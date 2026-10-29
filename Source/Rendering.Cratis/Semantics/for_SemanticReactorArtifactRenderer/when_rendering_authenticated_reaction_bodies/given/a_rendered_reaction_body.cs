// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies.given;

public class a_rendered_reaction_body : Specification
{
    protected ArtifactRenderPlan Plan = null!;

    protected string? Handler => Plan.Artifacts
        .Where(artifact => artifact.RelativePath.EndsWith("Notifier.cs", StringComparison.Ordinal))
        .Select(artifact => Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))
        .SingleOrDefault();

    protected void Render(string source) => Plan = opaque_policy_model.Plan(reaction_body_model.LoadSource(source));

    protected void ShouldPublishAndCompile(string body)
    {
        Assert.True(Plan.CanPublishArtifacts, opaque_policy_model.Errors(Plan));
        Assert.Contains(body, Handler, StringComparison.Ordinal);
        Assert.Empty(RenderedOutput.Errors(opaque_policy_model.Files(Plan)));
        Assert.Empty(RenderedOutput.Warnings(opaque_policy_model.Files(Plan)));
    }

    protected void ShouldRefuseWith(string diagnosticId)
    {
        Assert.False(Plan.CanPublishArtifacts);
        Assert.Empty(Plan.Artifacts);
        Assert.Contains(Plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error && diagnostic.Message.Contains(diagnosticId, StringComparison.Ordinal));
    }
}
#endif
