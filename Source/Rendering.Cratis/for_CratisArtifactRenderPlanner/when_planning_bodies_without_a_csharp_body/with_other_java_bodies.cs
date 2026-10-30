// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_body_carrying_command_handlers;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_bodies_without_a_csharp_body;

public class with_other_java_bodies
{
    [Theory]
    [InlineData("handler", ImplementationGapMode.Refuse, false)]
    [InlineData("handler", ImplementationGapMode.Refuse, true)]
    [InlineData("handler", ImplementationGapMode.Stub, false)]
    [InlineData("handler", ImplementationGapMode.Stub, true)]
    [InlineData("reaction", ImplementationGapMode.Refuse, false)]
    [InlineData("reaction", ImplementationGapMode.Refuse, true)]
    [InlineData("reaction", ImplementationGapMode.Stub, false)]
    [InlineData("reaction", ImplementationGapMode.Stub, true)]
    [InlineData("reducer", ImplementationGapMode.Refuse, false)]
    [InlineData("reducer", ImplementationGapMode.Refuse, true)]
    [InlineData("reducer", ImplementationGapMode.Stub, false)]
    [InlineData("reducer", ImplementationGapMode.Stub, true)]
    public async Task should_refuse_without_a_fallback(string role, ImplementationGapMode mode, bool strict)
    {
        ArtifactRenderPlan plan;
        SemanticId owner;
        if (role == "handler")
        {
            var request = with_attached_bodies_beside_productions.Request(language: "java");
            owner = Assert.Single(request.ImplementationRequirements).Source.SemanticId;
            plan = CratisRendering.Plan(request.Model, request.ExecutionPlan, request.Scope, new("Described", "Described") { ImplementationGaps = mode, Strict = strict }, request.ImplementationRequirements, request.ImplementationContents, [], request.TypedContextDescriptors);
        }
        else
        {
            var loaded = role == "reaction" ? reaction_body_model.Load() : await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.Source);
            var slices = loaded.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).ToArray();
            owner = role == "reaction" ? slices.SelectMany(slice => slice.Reactions).Single().Id : slices.SelectMany(slice => slice.Reducers).Single().ReadModel;
            plan = Plan(loaded, mode, strict);
        }

        Assert.Equal(ArtifactRenderOutcome.Refused, plan.Outcome);
        var diagnostic = Assert.Single(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-LANGUAGE");
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(owner, diagnostic.Artifact);
        Assert.Empty(plan.Artifacts);
        Assert.Empty(plan.ImplementationGaps);
        Assert.DoesNotContain(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-IMPL-006" || diagnostic.Code == "STAGE-ESM-019" || diagnostic.Code == "STAGE-INLINE-INTEGRITY");
    }

    static ArtifactRenderPlan Plan(LoadedSemanticModel loaded, ImplementationGapMode mode, bool strict)
    {
        var requirement = Assert.Single(loaded.ImplementationRequirements) with { Language = "java", File = null };
        return CratisRendering.Plan(loaded.Model, loaded.Plan, new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id), new("Bodies", "Bodies") { ImplementationGaps = mode, Strict = strict }, [requirement], loaded.ImplementationContents, [], loaded.TypedContextDescriptors);
    }
}
#endif
