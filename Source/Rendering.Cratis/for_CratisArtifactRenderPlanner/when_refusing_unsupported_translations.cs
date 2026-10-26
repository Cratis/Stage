// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_unsupported_translations : given.a_translation_application
{
    [Theory]
    [InlineData("TrackShipments")]
    [InlineData("PublishOrderShipped")]
    public void should_not_admit_non_translation_uses_just_because_the_contract_participates_elsewhere(string name)
    {
        var slice = _model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Name == name);
        var contract = slice.Events.Single(candidate => candidate.Origin is not null || candidate.Visibility == SemanticEventVisibility.Public);
        var options = new CratisRenderingOptions("Contracts", "Contracts");
        var request = new ArtifactRenderRequest(_model, SemanticExecutionPlan.Compile(_model).Plan!, CratisRendering.CreateProfile(_model.Application.Name, options), new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        SemanticTranslationAdmission.AdmitsEvent(contract.Id, slice with { Kind = SemanticSliceKind.StateChange }, context).ShouldBeFalse();
        SemanticTranslationAdmission.AdmitsEvent(contract.Id, slice with { Kind = SemanticSliceKind.StateChange, Captures = [], Projections = [] }, context).ShouldBeTrue();
    }

    [Theory]
    [InlineData(SemanticCaptureConditionKind.Added)]
    [InlineData(SemanticCaptureConditionKind.AnyChanged)]
    [InlineData(SemanticCaptureConditionKind.AllChanged)]
    [InlineData(SemanticCaptureConditionKind.Removed)]
    public void should_render_guards_decided_from_durable_capture_state(SemanticCaptureConditionKind condition)
    {
        var plan = Plan(Guarded(new(condition, condition is SemanticCaptureConditionKind.Removed or SemanticCaptureConditionKind.Added ? [] : ["orderId"])));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
        plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith("ShipmentTracking.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void should_refuse_an_expression_guard_by_name()
    {
        var plan = Plan(Guarded(new(SemanticCaptureConditionKind.Expression, []) { Expression = "orderId != null" }));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024" && diagnostic.Message.Contains("expression guard 'orderId != null'", StringComparison.Ordinal));
    }

    [Fact]
    public void should_refuse_a_guard_path_the_reference_evaluator_cannot_traverse()
    {
        var plan = Plan(Guarded(new(SemanticCaptureConditionKind.AnyChanged, ["order.id"])));
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024" && diagnostic.Message.Contains("'order.id'", StringComparison.Ordinal));
    }

    [Fact]
    public void should_refuse_a_transition_over_a_value_whose_text_differs_from_the_model()
    {
        var plan = Plan(Guarded(new(SemanticCaptureConditionKind.Transition, ["orderId"]) { From = "a", To = "b" }));
        plan.Success.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024" && diagnostic.Message.Contains("transition guard field 'orderId'", StringComparison.Ordinal));
    }

    [Fact]
    public void should_render_the_canonical_changed_transition_and_overlay_capture()
    {
        var model = Transform(SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.ToArray()), slice => slice with { Specifications = [] });
        var plan = Plan(model);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
        plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith("Feed.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void should_refuse_untyped_field_coercion_before_emitting_a_constructor_call()
    {
        var model = Transform(_model, slice => slice.Name == "TrackShipments" ? slice with
        {
            Events = [.. slice.Events.Select(contract => contract.Origin is null ? contract : contract with
            {
                Properties = [.. contract.Properties.Select(property => property with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) })]
            })]
        } : slice);
        var plan = Plan(model);
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024" &&
            diagnostic.Message.Contains("ShipmentDispatched.orderId", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("OrderDispatched.orderId", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("different types", StringComparison.Ordinal));
        plan.Artifacts.ShouldBeEmpty();
    }

    [Fact]
    public void should_refuse_an_external_system_capture()
    {
        var plan = Plan(Transform(_model, slice => slice with { Captures = [.. slice.Captures.Select(capture => capture with { EventsSource = null })] }));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
    }

    [Fact]
    public void should_not_admit_an_orphan_public_contract()
    {
        var plan = Plan(Transform(_model, slice => slice.Kind == SemanticSliceKind.Translate && slice.Direction == SemanticTranslationDirection.Outbound ? slice with { Projections = [] } : slice));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-032");
    }

    [Fact]
    public void should_keep_custom_public_projection_keys_rejected_by_semantic_validation()
    {
        var sourceProperty = _model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "PackOrder").Events.Single().Properties.Single().Id;
        var error = Assert.Throws<InvalidSemanticContract>(() => Transform(_model, slice => slice with
        {
            Projections = [.. slice.Projections.Select(projection => projection with
            {
                Scope = projection.Scope! with
                {
                    From = [.. projection.Scope!.From.Select(binding => binding with { Key = new SemanticProjectionValueKey(new SemanticProjectionEventProperty([sourceProperty])) })]
                }
            })]
        }));
        Assert.Contains("event source identity", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void should_refuse_a_publication_name_that_collides_with_its_comparison_member()
    {
        var plan = Plan(Transform(_model, slice => slice with { Projections = [.. slice.Projections.Select(projection => projection with { Name = "SamePublicValue" })] }));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("SamePublicValue", StringComparison.Ordinal));
    }

    [Fact]
    public void should_keep_event_target_reducers_refused_even_beside_a_supported_publication()
    {
        var plan = Plan(Transform(_model, slice => slice.Projections.IsEmpty ? slice : slice with
        {
            Reducers = [new("OtherPublication", slice.Projections.Single().ReadModel, [new(slice.Projections.Single().Scope!.From[0].EventContract, "other-publication")]) { Target = SemanticProjectionTargetKind.Event }]
        }));
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-032" && diagnostic.Message.Contains("OtherPublication", StringComparison.Ordinal));
    }

    [Fact]
    public void should_render_direct_translation_specifications_without_a_local_log_publication()
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.ToArray());
        var plan = Plan(model, ArtifactRefusalMode.RenderAround);
        plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Complete);
        plan.Success.ShouldBeTrue();
        var specifications = model.Application.Modules.Single().Features.Single().Slices
            .Where(slice => slice.Kind == SemanticSliceKind.Translate).SelectMany(slice => slice.Specifications).ToArray();
        specifications.ShouldNotBeEmpty();
        plan.Refusals.ShouldBeEmpty();
        foreach (var specification in specifications)
        {
            plan.Artifacts.ShouldContain(artifact => artifact.Sources.Contains(specification.Id));
        }
        plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith("ShipmentTracking.cs", StringComparison.Ordinal));
        plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith("OrderShippedPublisher.cs", StringComparison.Ordinal));
    }

    ExecutableSemanticModel Guarded(SemanticCaptureCondition when) => Transform(_model, slice => slice with
    {
        Captures = [.. slice.Captures.Select(capture => capture with { Appends = [.. capture.Appends.Select(append => append with { When = when })] })]
    });
}
#endif
