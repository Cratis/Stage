// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_a_composite_stream_id : given.a_routed_command
{
    void Establish()
    {
        var uuid = _command.Properties[0];
        var month = _command.Properties[1];
        _source = _source with { Streams = [_source.Streams[0] with { StreamIdType = null, StreamIdParts = [new("account", uuid.Type), new("month", month.Type), new("label", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))] }] };
        _command = _command with
        {
            Route = _command.Route! with
            {
                StreamId = null,
                StreamIdParts = [new("account", SemanticExpression.Property(SemanticExpressionRootKind.Command, uuid.Id)), new("month", _command.Route.StreamId!), new("label", SemanticExpression.FromValue(SemanticValue.Text("a|b%")))]
            }
        };
    }
    void Because() => Plan();

    [Fact] void should_format_parts_in_declaration_order() => _code.Contains("StreamIds.Composite(global::InvoiceApp.GeneratedEventSources.StreamIds.Uuid(AccountId.Value), global::InvoiceApp.GeneratedEventSources.StreamIds.Integer(Month.Value), \"a|b%\")", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_compile() => RenderedOutput.Errors(Files()).ShouldBeEmpty();
}
