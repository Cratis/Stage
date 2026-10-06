// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_initial_events_in_a_v4_model : Specification
{
    ExecutableSemanticModel _model = null!;
    ArtifactRenderScope _scope = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource) + "\n" + """
                slice StateChange ChangeOther
                  command ChangeOther
                    id Uuid identifier
                    description String
                    produces OtherChanged
                      for id
                      description = description
                  event OtherChanged generation 1
                    oldDescription String
                  event OtherChanged generation 2
                    description String
            """;
        _model = invoice_model.Compile(source);
        var slice = _model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Issue");
        _scope = new(ArtifactRenderScopeKind.Slice, slice.Id);
    }

    void Because() => _plan = invoice_model.Plan(_model, _scope);

    [Fact] void should_keep_the_v4_version_gate_open() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V4);
    [Fact] void should_render_the_unrelated_initial_event_scope() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_emit_the_initial_event_with_the_unchanged_attribute() => _plan.Artifacts.Any(artifact => System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).Contains("[global::Cratis.Chronicle.Events.EventTypeAttribute]\npublic record InvoiceIssued", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_refuse_the_whole_evolved_application() => invoice_model.Plan(_model).Diagnostics.Single().Code.ShouldEqual("STAGE-ESM-026");
}
