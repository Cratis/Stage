// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.EventSources;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticEventSourceArtifactRenderer;

public class when_reading_back_generated_definitions : a_routed_command
{
    EventSourceDefinition _definition = null!;
    Type _declared = null!;
    global::Cratis.Arc.Chronicle.Commands.IEventSourceDeclaration _commandRoute = null!;

    void Because()
    {
        Plan();
        var assembly = Load();
        _declared = assembly.GetType("InvoiceApp.EventSources.AccountEventSource")!;
        _commandRoute = assembly.GetType("InvoiceApp.Banking.Deposits.Deposit.Deposit")!.GetCustomAttributes(false).OfType<global::Cratis.Arc.Chronicle.Commands.IEventSourceDeclaration>().Single();
        _definition = (EventSourceDefinition)typeof(global::Cratis.Chronicle.EventSources.EventSources).GetMethod("Describe", BindingFlags.Static | BindingFlags.NonPublic, [typeof(Type)])!.Invoke(null, [_declared])!;
    }

    [Fact] void should_be_a_chronicle_source_definition() => typeof(IEventSource).IsAssignableFrom(_declared).ShouldBeTrue();
    [Fact] void should_retain_the_stored_source_name() => _definition.Name.ShouldEqual("Account");
    [Fact] void should_retain_the_stored_stream_name() => _definition.Streams.Single().Name.ShouldEqual("Transactions");
    [Fact] void should_preserve_a_declared_literal_stream_constant() => _declared.GetField("Transactions")!.IsLiteral.ShouldBeTrue();
    [Fact] void should_recover_the_stored_stream_from_the_constant() => _declared.GetField("Transactions")!.GetRawConstantValue().ShouldEqual("Transactions");
    [Fact] void should_resolve_the_command_stream_attribute() => _commandRoute.Stream.ShouldEqual("Transactions");
    [Fact] void should_resolve_the_command_source_type() => _commandRoute.EventSource.ShouldEqual(_declared);
    [Fact] void should_use_no_source_concurrency_dimensions() => _definition.Concurrency.ShouldEqual(ConcurrencyDimensions.None);
    [Fact] void should_use_no_stream_concurrency_dimensions() => _definition.Streams.Single().Concurrency.ShouldEqual(ConcurrencyDimensions.None);
}
