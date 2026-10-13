// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticEventSourceArtifactRenderer.when_generating_event_sources;

public class and_stored_names_differ_from_the_names : a_routed_command
{
    string _definition = null!;
    Type _declared = null!;
    global::Cratis.Arc.Chronicle.Commands.IEventSourceDeclaration _commandRoute = null!;

    void Establish() => _source = _source with { Name = "OrderItem", SourceKind = "Item", Streams = [_source.Streams[0] with { Name = "Primary", StreamKind = "Orders" }] };
    void Because()
    {
        Plan();
        _definition = Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "EventSources/OrderItemEventSource.cs"));
        var assembly = Load();
        _declared = assembly.GetType("InvoiceApp.EventSources.OrderItemEventSource")!;
        _commandRoute = assembly.GetType("InvoiceApp.Banking.Deposits.Deposit.Deposit")!.GetCustomAttributes(false).OfType<global::Cratis.Arc.Chronicle.Commands.IEventSourceDeclaration>().Single();
    }

    [Fact] void should_name_the_class_after_the_declaration() => _definition.Contains("class OrderItemEventSource", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_stored_source_type() => _definition.Contains("EventSourceAttribute(\"Item\")", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_stored_stream_type() => _definition.Contains("EventStreamAttribute(global::InvoiceApp.EventSources.OrderItemEventSource.Primary)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_recover_the_stored_stream_value() => _declared.GetField("Primary")!.GetRawConstantValue().ShouldEqual("Orders");
    [Fact] void should_resolve_the_stored_stream_on_the_command() => _commandRoute.Stream.ShouldEqual("Orders");
    [Fact] void should_resolve_the_stored_source_attribute() => _declared.GetCustomAttributes(false).OfType<global::Cratis.Chronicle.EventSources.EventSourceAttribute>().Single().Name.ShouldEqual("Item");
    [Fact] void should_resolve_the_stored_stream_attribute() => _declared.GetCustomAttributes(false).OfType<global::Cratis.Chronicle.EventSources.EventStreamAttribute>().Single().Name.ShouldEqual("Orders");
    [Fact] void should_declare_the_stored_stream_constant() => _definition.Contains("public const string Primary = \"Orders\";", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_stored_stream_on_the_command() => _code.Contains("OrderItemEventSource>(global::InvoiceApp.EventSources.OrderItemEventSource.Primary)", StringComparison.Ordinal).ShouldBeTrue();
}
