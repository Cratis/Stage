// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_unobservable_event_sources
{
    [Fact]
    void should_render_givens_and_the_append_on_one_placeholder()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(when_checking_sourceless_event_consumers.Source));
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        var source = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("when_recreate_is_projected.cs", StringComparison.Ordinal)).Bytes.AsSpan());
        Assert.Equal(2, source.Split("ForEventSource(\"when-append\")", StringSplitOptions.None).Length - 1);
    }

    [Theory]
    [InlineData(false, "RecreateRemovedParts")]
    [InlineData(true, "Recreate")]
    void should_render_the_composite_keyed_recreation(bool golden, string name)
    {
        var bytes = golden ? EsmGoldenVectors.Get(27).Bytes : CompositeReadModelKeysCorpus.V27.EsmBytes;
        var model = SemanticModelSerializer.Deserialize(bytes.AsSpan());
        var slice = SemanticVersionFeatures.Slices(model.Application).Single();
        var specification = slice.Specifications.Single(selected => selected.Name == name);
        var options = new CratisRenderingOptions("Payments", "Payments");
        var context = new SemanticApplicationContext(
            new(model, SemanticExecutionPlan.Compile(model, []).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id)),
            options);
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticSpecificationAdmission.Validate(context, slice with { Specifications = [specification] }, diagnostics);
        Assert.Empty(diagnostics);
        Assert.True(SemanticProjectionFactSpecification.TryLower(context, specification, out var lowered));
        var rendered = SemanticReadModelSpecificationRenderer.Render(lowered!, specification.ThenReadModels.Single(), context);
        Assert.Contains("ForEventSource(\"when-append\")", rendered.Content, StringComparison.Ordinal);
        Assert.Contains("SingleOrDefault(_scenario.Instances.Values", rendered.Content, StringComparison.Ordinal);
    }
}
