// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_translation_contracts : given.a_translation_application
{
    void Because() => Render();

    [Fact] void should_compile_the_actual_backend_without_errors() => RenderedOutput.Errors(_sources).ShouldBeEmpty();
    [Fact] void should_compile_the_actual_backend_without_warnings() => RenderedOutput.Warnings(_sources).ShouldBeEmpty();
    [Fact] void should_move_the_local_public_contract_to_the_contracts_project() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.StartsWith("Contracts.Events.Contracts/", StringComparison.Ordinal) && artifact.RelativePath.EndsWith("/PublishOrderShipped.Events.cs", StringComparison.Ordinal));
    [Fact] void should_name_the_event_store_on_the_contracts_assembly() => System.Text.Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath == "Contracts.Events.Contracts/Contracts.Events.Contracts.csproj").Bytes.AsSpan()).ShouldContain("Cratis.Chronicle.Events.EventStoreAttribute");
    [Fact] void should_keep_the_namespace_and_drop_the_per_type_public_attribute_of_the_moved_event() => Generated("PublishOrderShipped", "OrderShipped").GetCustomAttributes(typeof(PublicAttribute), false).ShouldBeEmpty();
    [Fact] void should_preserve_the_opaque_foreign_origin() => ((EventStoreAttribute)Generated("TrackShipments", "ShipmentDispatched").GetCustomAttributes(typeof(EventStoreAttribute), false).Single()).EventStore.ShouldEqual("shipping");
    [Fact] void should_leave_private_facts_private() => Generated("TrackShipments", "OrderDispatched").GetCustomAttributes(typeof(PublicAttribute), false).ShouldBeEmpty();
    [Fact] void should_observe_the_inbox_for_the_capture() => Generated("TrackShipments", "ShipmentTracking").GetEventSequenceId().ShouldEqual(new EventSequenceId("inbox-shipping"));
    [Fact] void should_leave_no_refused_specifications_when_none_were_requested() => _plan.Refusals.ShouldBeEmpty();

    [Theory]
    [InlineData("TrackShipments")]
    [InlineData("PublishOrderShipped")]
    public void should_keep_contract_and_reactor_files_distinct_when_the_reactor_has_the_slice_name(string name)
    {
        _model = Transform(_model, slice => slice.Name != name ? slice : slice with
        {
            Captures = [.. slice.Captures.Select(capture => capture with { Name = name })],
            Projections = [.. slice.Projections.Select(projection => projection with { Name = name })]
        });
        Render();
        _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith($"/{name}.Events.cs", StringComparison.Ordinal));
        _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith($"/{name}.cs", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("TrackShipments", "ShipmentTracking.cs")]
    [InlineData("PublishOrderShipped", "OrderShippedPublisher.cs")]
    public void should_support_slice_scoped_translation(string name, string reactor)
    {
        var slice = _model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Name == name);
        var plan = CratisRendering.Plan(_model, SemanticExecutionPlan.Compile(_model).Plan!, new(ArtifactRenderScopeKind.Slice, slice.Id), new("Contracts", "Contracts"));
        plan.Success.ShouldBeTrue();
        plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.EndsWith(reactor, StringComparison.Ordinal));
        plan.Artifacts.ShouldNotContain(artifact => artifact.RelativePath.EndsWith(name == "TrackShipments" ? "OrderShippedPublisher.cs" : "ShipmentTracking.cs", StringComparison.Ordinal));
    }
}
#endif
