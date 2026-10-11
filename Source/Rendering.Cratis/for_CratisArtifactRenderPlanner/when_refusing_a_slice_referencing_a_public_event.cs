// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_a_slice_referencing_a_public_event : Specification
{
    [Theory]
    [InlineData("given")]
    [InlineData("then")]
    [InlineData("append")]
    public void should_refuse_a_reference_without_selecting_the_declaring_slice(string form)
    {
        var original = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var module = original.Application.Modules.Single();
        var feature = module.Features.Single();
        var selected = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateChange);
        var publisher = feature.Slices.Single(slice => slice.Direction == SemanticTranslationDirection.Outbound);
        var specification = publisher.Specifications.Single();
        var occurrence = specification.ThenEvents.Single();
        specification = form switch
        {
            "given" => specification with { GivenEvents = [occurrence] },
            "append" => specification with { WhenAppended = new(occurrence.EventContract, occurrence.Values) },
            _ => specification
        };
        var application = original.Application with
        {
            Modules = [module with { Features = [feature with
            {
                Slices = [.. feature.Slices.Select(slice => slice.Id == selected.Id
                    ? slice with { Specifications = [specification] }
                    : slice with { Specifications = [] })]
            }] }]
        };
        var model = ExecutableSemanticModel.Create(original.LanguageVersion, original.SemanticVersion, application);
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Slice, selected.Id), new("Contracts", "Contracts"));
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-031"]);
        plan.Artifacts.ShouldBeEmpty();
    }

    [Fact]
    public void should_refuse_a_projection_reference()
    {
        var corpus = PublicEventsCorpus.V9;
        var source = corpus.SourceForms.Single().Documents.Single().Text + "\n" +
            "        slice StateView Shipments\n            readmodel Shipment\n                id String\n                carrier String\n                status OrderStatus\n            query ShipmentById => Shipment?\n                by id String\n            projection ShipmentView => Shipment\n                from OrderShipped key carrier\n                    id = carrier\n                    carrier = carrier\n                    status = status";
        var catalog = SemanticIdentityCatalog.Empty(corpus.ApplicationIdentity);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("public-reference"), "public-reference", "PublicReference.play", source);
        var compilation = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Shipments");
        var plan = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Slice, slice.Id), new("Contracts", "Contracts"));
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly(["STAGE-ESM-031"]);
        plan.Artifacts.ShouldBeEmpty();
    }
}
