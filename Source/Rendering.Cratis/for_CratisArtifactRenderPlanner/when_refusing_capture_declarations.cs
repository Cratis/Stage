// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_capture_declarations : given.a_translation_application
{
    [Theory]
    [InlineData("Capture@Tail")]
    [InlineData("Capture!")]
    [InlineData("Capture/*comment*/")]
    [InlineData("Capture;Other")]
    public void should_refuse_invalid_generated_capture_identifiers_explicitly(string name)
    {
        var model = Transform(_model, slice => slice with { Captures = [.. slice.Captures.Select(capture => capture with { Name = name })] });
        var plan = Plan(model);
        given.rendering_refusals.Rejected(plan, "STAGE-ESM-004");
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-004" && diagnostic.Message.Contains("not a C# identifier", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("@ShipmentDispatched")]
    [InlineData("ShipmentDispat\\u0063hed")]
    [InlineData("ShipmentDispat\u200cched")]
    public void should_refuse_decoded_capture_event_type_collisions_in_application_and_slice_scopes(string name)
    {
        var model = Transform(_model, slice => slice with { Captures = [.. slice.Captures.Select(capture => capture with { Name = name })] });
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Name == "TrackShipments");
        var application = Plan(model);
        var selected = CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Slice, slice.Id), new("Contracts", "Contracts"));
        foreach (var plan in new[] { application, selected })
        {
            given.rendering_refusals.Rejected(plan, "STAGE-ESM-012");
            Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Artifact == slice.Captures.Single().Id && diagnostic.Code == "STAGE-ESM-012");
        }
    }

    [Fact]
    public void should_refuse_repeated_sources_in_the_declaration_plan_even_before_model_validation()
    {
        var slice = _model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Name == "TrackShipments");
        var capture = slice.Captures.Single();
        var options = new CratisRenderingOptions("Contracts", "Contracts");
        var context = new SemanticApplicationContext(new(_model, SemanticExecutionPlan.Compile(_model).Plan!, CratisRendering.CreateProfile(_model.Application.Name, options), new(ArtifactRenderScopeKind.Application, _model.Application.Id)), options);
        var plan = SemanticCaptureMemberPlan.Create(capture with { EventsSource = capture.EventsSource! with { Events = [capture.EventsSource.Events[0], capture.EventsSource.Events[0]] } }, context);
        Assert.Contains(plan.Refusals, reason => reason.Contains("source event is repeated", StringComparison.Ordinal));
    }

    [Fact]
    public void should_refuse_the_same_qualified_signature_in_an_isolated_declaration_plan()
    {
        var probe = InternalCaptureProbe(sameNamespace: true);
        var plan = probe.Context.CaptureMembers(probe.Capture);
        Assert.NotEqual(probe.First, probe.Second);
        Assert.Contains(plan.Refusals, reason => reason.Contains("same emitted inbox handler parameter type", StringComparison.Ordinal));
    }

    [Fact]
    public void should_still_refuse_unpinned_same_name_sources_through_normal_admission()
    {
        var probe = InternalCaptureProbe(sameNamespace: false);
        var plan = Plan(probe.Context.Request.Model);
        given.rendering_refusals.Rejected(plan, "STAGE-ESM-012");
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("Chronicle event type id", StringComparison.Ordinal));
    }

    [Fact]
    public void should_refuse_a_decoded_capture_namespace_collision()
    {
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(candidate => candidate.Name == "TrackShipments");
        var nested = slice with { Id = SemanticId.Create(SemanticKind.Slice, "nested"), Name = "Nested", Events = [], Captures = [], Specifications = [] };
        var child = feature with { Id = SemanticId.Create(SemanticKind.Feature, "track-namespace"), Name = "TrackShipments", Slices = [nested], Features = [] };
        var model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with
        {
            Modules = [module with { Features = [feature with { Features = [child], Slices = [.. feature.Slices.Select(candidate => candidate with { Captures = [.. candidate.Captures.Select(capture => capture with { Name = "@Nested" })] })] }] }]
        });
        var plan = Plan(model);
        given.rendering_refusals.Rejected(plan, "STAGE-ESM-012");
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-012" && diagnostic.Message.Contains("namespace", StringComparison.Ordinal));
    }
}
#endif
