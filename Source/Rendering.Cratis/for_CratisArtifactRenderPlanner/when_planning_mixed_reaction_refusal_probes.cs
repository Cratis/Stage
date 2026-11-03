// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_mixed_reaction_refusal_probes
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_emit_each_relative_path_once(bool atomic)
    {
        var referenceModel = an_automation.Compile((atomic ? "reaction batches atomic\n" : string.Empty) + MixedReactionRefusalProbeSource.Model);
        var module = referenceModel.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var model = ExecutableSemanticModel.Create(referenceModel.LanguageVersion, referenceModel.SemanticVersion, referenceModel.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [] }] }] }]
        });
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Reactions", "Reactions"));
        Assert.True(plan.Success, string.Join(';', plan.Diagnostics));
        Assert.Empty(plan.Artifacts.GroupBy(artifact => artifact.RelativePath, StringComparer.Ordinal).Where(group => group.Count() > 1).Select(group => group.Key));
    }
}
