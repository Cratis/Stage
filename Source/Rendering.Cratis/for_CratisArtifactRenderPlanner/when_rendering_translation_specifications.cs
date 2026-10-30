// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_translation_specifications : given.a_translation_specification_application
{
    void Because() => Render(clientOnly: true);

    [Fact] async Task should_compile_and_run_the_outbox_specification() => await RunGenerated("when_publishing_a_packed_order");
    [Fact] async Task should_compile_and_run_the_inbox_specification() => await RunGenerated("when_receiving_a_shipment");
    [Fact]
    void should_not_reference_unselected_foreign_contracts_in_the_publication_setup() =>
        _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith("when_publishing_a_packed_order.cs", StringComparison.Ordinal))
            .ShouldEachConformTo(artifact => !System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("ShipmentDispatched", StringComparison.Ordinal));

    [Fact] void should_not_blanket_refuse_translation_specifications() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-011").ShouldBeFalse();

    [Fact]
    void should_allow_a_public_expectation_outside_the_translation_slice()
    {
        var publication = _model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "PublishOrderShipped");
        var specification = publication.Specifications.Single();
        var changed = Transform(_model, slice => slice.Name == "PackOrder" ? slice with { Specifications = [specification with { Id = SemanticId.Create(SemanticKind.Specification, "outside-translation") }] } : slice);
        var plan = Plan(changed, ArtifactRefusalMode.RenderAround);
        plan.Refusals.Any(refusal => refusal.Codes.Contains("STAGE-ESM-032") && refusal.Artifact == publication.Events.Single().Id).ShouldBeFalse();
    }

    [Fact]
    void should_omit_a_specification_with_cascaded_outcomes()
    {
        var changed = Transform(_model, slice => slice.Name == "PublishOrderShipped" ? slice with
        {
            Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                ThenEvents = [.. specification.ThenEvents, new SemanticSpecificationEvent(specification.WhenAppended!.EventContract, specification.WhenAppended.Values)]
            })]
        } : slice);
        var plan = Plan(changed);
        AssertOmittedSpecification(changed, plan);
        plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning).ShouldBeTrue();
    }

    [Fact]
    void should_omit_a_specification_whose_history_requires_settling_the_translation()
    {
        var changed = Transform(_model, slice => slice.Name == "PublishOrderShipped" ? slice with
        {
            Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                GivenEvents = [new(specification.WhenAppended!.EventContract, specification.WhenAppended.Values)
                {
                    EventSource = specification.WhenAppended.EventSource
                }]
            })]
        } : slice);
        var plan = Plan(changed);
        AssertOmittedSpecification(changed, plan);
        plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Warning && diagnostic.Message.Contains("given history must contain", StringComparison.Ordinal)).ShouldBeTrue();
    }

    static void AssertOmittedSpecification(ExecutableSemanticModel model, ArtifactRenderPlan plan)
    {
        var specification = model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "PublishOrderShipped").Specifications.Single();
        plan.Success.ShouldBeTrue();
        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
        plan.Refusals.Single().Artifact.ShouldEqual(specification.Id);
        plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(specification.Id);
    }
}
#endif
