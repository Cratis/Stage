// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_an_unrendered_strategy : Specification
{
    [Fact]
    public void should_gap_an_unsupported_byte_encoding_without_aborting_other_concepts()
    {
        var model = a_generation_model.Model();
        var execution = SemanticExecutionPlan.Compile(model).Plan!;
        var concept = model.Application.Concepts.Single(concept => concept.Name == "Receipt");
        var generation = concept.Generation!;
        typeof(SemanticGeneration).GetField("<Kind>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(generation, "bytes");
        typeof(SemanticGeneration).GetField("<Encoding>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(generation, "future");
        var plan = CratisRendering.Plan(model, execution, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("GenerationApp", "GenerationApp"));
        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
        plan.Diagnostics.Any(diagnostic => diagnostic.Artifact == concept.Id && diagnostic.Message == "Byte encoding 'future' is not admitted." && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldBeTrue();
        plan.ImplementationGaps.Any(gap => gap.Owner == concept.Id.ToString() && gap.Kind == ArtifactImplementationGapKind.Rendering).ShouldBeTrue();
        plan.Artifacts.Any(artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md").ShouldBeTrue();
        rendering_refusals.Warning(plan, "STAGE-ESM-012", concept.Id, "Byte encoding 'future' is not admitted.");
        var factory = Assert.Single(plan.Artifacts.Where(artifact => artifact.Sources.Contains(concept.Id)));
        Assert.Contains("NotImplementedException", System.Text.Encoding.UTF8.GetString(factory.Bytes.AsSpan()), StringComparison.Ordinal);
        rendering_refusals.Retained(plan, model.Application.Concepts.Single(concept => concept.Name == "Id").Id);
#if DEBUG
        opaque_policy_model.Compile(plan).GetTypes().Length.ShouldBeGreaterThan(0);
#endif
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("Receipt")]
    public void should_refuse_without_falling_back_to_uuid(string name)
    {
        var model = a_generation_model.Model();
        var execution = SemanticExecutionPlan.Compile(model).Plan!;

        // Simulate a future compiler admitting a strategy this target has not audited.
        var generation = model.Application.Concepts.Single(concept => concept.Name == name).Generation!;
        typeof(SemanticGeneration).GetField("<Kind>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(generation, "future");
        var plan = CratisRendering.Plan(model, execution, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("GenerationApp", "GenerationApp"));
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-028");
        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
        plan.ImplementationGaps.Any(gap => gap.Owner == model.Application.Concepts.Single(concept => concept.Name == name).Id.ToString() && gap.Kind == ArtifactImplementationGapKind.Rendering).ShouldBeTrue();
#if DEBUG
        opaque_policy_model.Compile(plan).GetTypes().Length.ShouldBeGreaterThan(0);
#endif
    }
}
