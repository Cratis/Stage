// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

// A hand-written Customizations file written before the public event moved to the contracts project names the event by the
// namespace and type name it always had, so it must keep compiling and resolving to the same event type id.
public class when_a_customization_uses_a_moved_public_event : given.a_translation_application
{
    const string Customization = """
        using Contracts.Integration.Translation.PublishOrderShipped;

        namespace Contracts.Customizations;

        public static class OrderShippedCustomization
        {
            public static global::System.Type EventType => typeof(OrderShipped);

            public static string Describe(OrderShipped @event) => @event.GetType().FullName!;
        }
        """;

    RenderedFile[] _withCustomization = [];
    Type _customization = null!;

    void Because()
    {
        Render();
        _withCustomization = [.. _sources, new RenderedFile("Customizations/OrderShippedCustomization.cs", Customization)];
        _customization = RenderedOutput.Load(_withCustomization).GetType("Contracts.Customizations.OrderShippedCustomization", throwOnError: true)!;
    }

    [Fact] void should_keep_the_event_in_the_contracts_project() => _plan.Artifacts.ShouldContain(artifact => artifact.RelativePath.StartsWith("Contracts.Events.Contracts/", StringComparison.Ordinal) && artifact.RelativePath.EndsWith("/PublishOrderShipped.Events.cs", StringComparison.Ordinal));
    [Fact] void should_compile_the_customization_without_errors() => RenderedOutput.Errors(_withCustomization).ShouldBeEmpty();
    [Fact] void should_compile_the_customization_without_warnings() => RenderedOutput.Warnings(_withCustomization).ShouldBeEmpty();
    [Fact] void should_resolve_the_customization_to_the_event_with_the_same_identity() => ((Type)_customization.GetProperty("EventType")!.GetValue(null)!).ShouldEqual(Generated("PublishOrderShipped", "OrderShipped"));
    [Fact] void should_keep_the_event_type_attribute() => Generated("PublishOrderShipped", "OrderShipped").GetCustomAttributes(typeof(EventTypeAttribute), false).Length.ShouldEqual(1);
}
#endif
