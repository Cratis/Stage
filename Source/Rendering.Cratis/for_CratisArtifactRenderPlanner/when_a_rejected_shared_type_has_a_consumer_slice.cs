// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_rejected_shared_type_has_a_consumer_slice
{
    const string Source = """
        concept NotSet : String
        type Envelope
          value NotSet
        module Consumers
          feature Intake
            slice StateChange Receive
              command Receive
                id Uuid identifier
                payload Envelope
                produces Received
                  for id
              event Received
        """;

    [Fact]
    public void should_omit_the_composite_and_its_whole_consumer_slice_but_compile_independent_output()
    {
        var model = invoice_model.Compile(Source + a_render_with_an_invalid_typed_context.Independent);
        var consumer = model.Application.Modules.Single(module => module.Name == "Consumers").Features.Single().Slices.Single();
        var independent = model.Application.Modules.Single(module => module.Name == "Independent").Features.Single().Slices.Single();
        var concept = model.Application.Concepts.Single();
        var composite = model.Application.Types.Single();
        var plan = invoice_model.Plan(model);
        const string message = "Concept 'NotSet' has members that collide in generated C#.";
        rendering_refusals.Warning(plan, "STAGE-ESM-012", concept.Id, message);
        rendering_refusals.Omitted(plan, composite.Id);
        rendering_refusals.Omitted(plan, consumer.Id);
        rendering_refusals.Omitted(plan, consumer.Commands.Single().Id);
        rendering_refusals.Omitted(plan, consumer.Events.Single().Id);
        var dependency = Assert.Single(plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Artifact == consumer.Id));
        Assert.Equal(ArtifactRenderDiagnosticSeverity.Warning, dependency.Severity);
        Assert.EndsWith(message, dependency.Message, StringComparison.Ordinal);
        Assert.Contains(plan.ImplementationGaps, gap => gap.Kind == ArtifactImplementationGapKind.Rendering && gap.Owner == consumer.Id.ToString() && gap.Hints.Contains(dependency.Message));
        rendering_refusals.Retained(plan, independent.Commands.Single().Id);
        rendering_refusals.Retained(plan, independent.Events.Single().Id);
        rendering_refusals.Compiles(plan);
    }
}
#endif
