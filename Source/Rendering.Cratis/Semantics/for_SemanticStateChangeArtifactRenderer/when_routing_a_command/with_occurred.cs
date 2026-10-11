// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_occurred : given.a_routed_command
{
    void Establish()
    {
        var occurred = _slice.Events[0].Properties[0] with { Id = SemanticId.Parse($"sem1:{new string('c', 64)}"), Name = "occurred", Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DateTime) };
        _slice = _slice with { Events = [_slice.Events[0] with { Properties = [.. _slice.Events[0].Properties, occurred] }] };
        _command = _command with { Produces = [_command.Produces[0] with { Mappings = [.. _command.Produces[0].Mappings, new(occurred.Id, new SemanticEventContextExpression(SemanticEventContextValueKind.Occurred, occurred.Type))] }] };
    }
    void Because() => Plan();

    [Fact] void should_preserve_both_route_and_occurrence() => _code.Contains("EventStreamId = global::InvoiceApp.GeneratedEventSources.StreamIds.Integer(Month.Value), Occurred = occurred", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_read_the_dispatch_receipt() => _code.Contains("CommandReceiptTime.OccurredAtReceipt(operation)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_compile() => RenderedOutput.Errors(Files()).ShouldBeEmpty();
}
