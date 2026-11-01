// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_rejected_reaction_cascades;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_around_refusals;

public class with_an_unsettled_reaction_specification : given.a_refused_specification
{
    void Establish()
    {
        var source = "concept Ticket : Uuid\n  generate uuid\n" + with_a_triggering_command_without_rules.Source
            .Replace("        id String identifier\n        produces Started", "        id String identifier\n        ticket Ticket generated\n        produces Started", StringComparison.Ordinal);
        var model = for_CratisArtifactRenderPlanner.given.invoice_model.Compile(source);
        _refused = model.Application.Modules.Single().Features.Single().Slices.SelectMany(slice => slice.Specifications).Single().Id;
        _request = OptIn(new(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            CratisRendering.CreateProfile(model.Application.Name, new("InvoiceApp", "Invoices")),
            new(ArtifactRenderScopeKind.Application, model.Application.Id)));
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_render_a_complete_plan() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
    [Fact] void should_list_the_unsettled_specification() => _plan.Refusals.Single().Artifact.ShouldEqual(_refused);
    [Fact] void should_keep_the_reactor() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith("Worker.cs", StringComparison.Ordinal));
    [Fact] void should_omit_the_unsettled_specification() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_refused);
}
