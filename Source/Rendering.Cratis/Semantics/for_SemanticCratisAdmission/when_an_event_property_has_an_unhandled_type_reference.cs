// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission;

public class when_an_event_property_has_an_unhandled_type_reference : a_register_project_render_request
{
    SemanticId _eventId;
    ImmutableArray<ArtifactRenderDiagnostic> _diagnostics;
    ArtifactRenderPlan _plan = null!;
    UnsupportedSemanticRendering _error = null!;

    void Establish()
    {
        // Fault injection: simulate a future compiler's type variant on a valid event property.
        var @event = _registerProject.Events.Single();
        _eventId = @event.Id;
        var properties = @event.Properties.Select(property => property.Name == "name"
            ? property with { Type = property.Type with { Kind = (SemanticTypeReferenceKind)999 } }
            : property).ToImmutableArray();
        typeof(SemanticEventContract).GetField("<Properties>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(@event, properties);
        var profile = _request.Profile;
        _request = _request with
        {
            Profile = ArtifactRenderProfile.Create(
                profile.Target,
                profile.TargetVersion,
                profile.Renderer,
                profile.RendererVersion,
                [.. profile.Inputs.Where(input => input.Name != RefusalModeInput.Name), RefusalModeInput.Create(ArtifactRefusalMode.RenderAround)])
        };
    }

    void Because()
    {
        var context = new SemanticApplicationContext(_request, _options);
        _error = (UnsupportedSemanticRendering)Catch.Exception(() => SemanticCratisAdmission.TypeExists(
            context, _registerProject.Events.Single().Properties.Single(property => property.Name == "name").Type, _eventId));
        _diagnostics = SemanticCratisAdmission.Evaluate(context, context.AllSlices);
        _plan = _planner.Plan(_request);
    }

    [Fact] void should_capture_the_event_owner_on_the_exception() => _error.Owner.ShouldEqual(_eventId);
    [Fact] void should_mark_the_event_property_failure_as_whole_render_only() => _error.WholeRenderOnly.ShouldBeTrue();
    [Fact] void should_keep_the_exception_code() => _error.Code.ShouldEqual("STAGE-ESM-012");
    [Fact] void should_attribute_the_admission_refusal_to_the_event() => Unhandled(_diagnostics).Single().Artifact.ShouldEqual(_eventId);
    [Fact] void should_keep_the_admission_refusal_an_error() => Unhandled(_diagnostics).Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_preserve_the_exact_refusal_message() => Unhandled(_diagnostics).Single().Message.ShouldEqual("The Cratis ESM renderer cannot handle SemanticTypeReferenceKind '999'.");
    [Fact] void should_not_attribute_the_cause_to_the_slice_or_application() => Unhandled(_diagnostics)
        .Where(diagnostic => diagnostic.Artifact == _registerProject.Id || diagnostic.Artifact == _model.Application.Id).ShouldBeEmpty();
    [Fact] void should_refuse_the_whole_render_even_when_rendering_around_refusals() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
    [Fact] void should_attribute_the_plan_refusal_to_the_event_without_duplicates() => Unhandled(_plan.Diagnostics).Single().Artifact.ShouldEqual(_eventId);
    [Fact] void should_keep_the_plan_refusal_an_error() => Unhandled(_plan.Diagnostics).Single().Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_omit_any_artifacts() => _plan.Refusals.ShouldBeEmpty();

    static IEnumerable<ArtifactRenderDiagnostic> Unhandled(IEnumerable<ArtifactRenderDiagnostic> diagnostics) =>
        diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("999", StringComparison.Ordinal));
}
