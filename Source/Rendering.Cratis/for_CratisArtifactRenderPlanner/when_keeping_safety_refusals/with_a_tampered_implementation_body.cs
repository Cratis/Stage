// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_keeping_safety_refusals;

public class with_a_tampered_implementation_body
{
    [Theory]
    [InlineData(ImplementationGapMode.Refuse, ArtifactRefusalMode.Refuse)]
    [InlineData(ImplementationGapMode.Stub, ArtifactRefusalMode.Refuse)]
    [InlineData(ImplementationGapMode.Refuse, ArtifactRefusalMode.RenderAround)]
    [InlineData(ImplementationGapMode.Stub, ArtifactRefusalMode.RenderAround)]
    public void should_refuse_the_whole_render_without_containing_hash_integrity(ImplementationGapMode gaps, ArtifactRefusalMode refusals)
    {
        var loaded = validation_body_model.Load(validation_body_model.Command("return context.Value > 0;"));
        var requirement = loaded.ImplementationRequirements.Single();
        var command = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        var plan = CratisRendering.Plan(
            loaded.Model,
            loaded.Plan,
            new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id),
            new("Orders", "Orders") { ImplementationGaps = gaps, Refusals = refusals },
            loaded.ImplementationRequirements,
            loaded.ImplementationContents.SetItem(requirement.RequirementId, "return false;"),
            loaded.AttachmentDiagnostics,
            loaded.TypedContextDescriptors);

        Assert.Empty(plan.Artifacts);
        Assert.Equal(ArtifactRenderOutcome.Refused, plan.Outcome);
        Assert.Empty(plan.Refusals);
        Assert.Empty(plan.ImplementationGaps);
        var diagnostic = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-056"));
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(command.Id, diagnostic.Artifact);
        Assert.Contains(requirement.RequirementId, diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-020" && diagnostic.Message.Contains(requirement.RequirementId, StringComparison.Ordinal));
    }
}
#endif
