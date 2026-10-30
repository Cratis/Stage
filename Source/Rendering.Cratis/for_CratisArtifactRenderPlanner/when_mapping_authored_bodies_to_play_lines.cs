// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_mapping_authored_bodies_to_play_lines
{
    [Fact]
    void should_map_an_opaque_policy_body()
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source("var staff = context.Identity.IsAuthenticated;\nreturn staff && context.Subject != \"\";"));
        a_source_line_mapped_plan.AssertMapped(a_source_line_mapped_plan.Plan(loaded), loaded);
    }

    [Fact]
    void should_map_a_validation_body()
    {
        var loaded = validation_body_model.Load(validation_body_model.Command("return context.Value > 0;"));
        a_source_line_mapped_plan.AssertMapped(a_source_line_mapped_plan.Plan(loaded), loaded);
    }

    [Fact]
    void should_map_an_iterator_validation_body()
    {
        var loaded = validation_body_model.Load(validation_body_model.IteratorCommand("if (context.Artifact.Amount <= 0) yield return \"Positive amount required\";"));
        a_source_line_mapped_plan.AssertMapped(a_source_line_mapped_plan.Plan(loaded), loaded);
    }

    [Fact]
    void should_map_a_reaction_body()
    {
        var loaded = Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies.reaction_body_model.LoadSource(native_reaction_model.Source());
        a_source_line_mapped_plan.AssertMapped(a_source_line_mapped_plan.Plan(loaded), loaded);
    }

    [Fact]
    async Task should_map_a_reducer_transition()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(when_rendering_a_pure_reducer.Source);
        a_source_line_mapped_plan.AssertMapped(a_source_line_mapped_plan.Plan(loaded, application: "Projects", project: "Projects"), loaded);
    }

    [Fact]
    async Task should_map_a_query_performer_body()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(a_source_line_mapped_plan.PerformerSource("return readModels;"));
        a_source_line_mapped_plan.AssertMapped(a_source_line_mapped_plan.Plan(loaded, application: "Projects", project: "Projects"), loaded);
    }

    [Fact]
    void should_refuse_a_duplicate_mapping_input()
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source("return context.Identity.IsAuthenticated;"));
        var mapped = CratisRendering.WithSourceLineMapping(CratisRendering.CreateProfile(loaded.Model.Application.Name, new("InvoiceApp", "Invoices")), "Model", []);
        var duplicate = ArtifactRenderProfile.Create(
            mapped.Target,
            mapped.TargetVersion,
            mapped.Renderer,
            mapped.RendererVersion,
            [.. mapped.Inputs, ArtifactRenderInput.Create(SourceLineMappingInput.Name, "2", SourceLineMappingInput.Create("Model", []).Bytes)]);
        var plan = a_source_line_mapped_plan.PlanWith(loaded, duplicate);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Message.Contains("invalid or duplicate source line mapping", StringComparison.Ordinal));
    }

    [Fact]
    void should_emit_no_directives_without_the_input()
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source("return context.Identity.IsAuthenticated;"));
        var plan = a_source_line_mapped_plan.Plan(loaded, mapped: false);
        Assert.DoesNotContain(plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)), artifact => a_source_line_mapped_plan.Lines(artifact).Any(line => line.StartsWith("#line", StringComparison.Ordinal)));
    }
}
#endif
