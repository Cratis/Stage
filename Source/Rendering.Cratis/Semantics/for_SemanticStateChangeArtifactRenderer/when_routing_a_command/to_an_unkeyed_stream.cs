// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class to_an_unkeyed_stream : given.a_routed_command
{
    void Establish()
    {
        _source = _source with { Streams = [_source.Streams[0] with { StreamIdType = null }] };
        _command = _command with { Route = _command.Route! with { StreamId = null } };
    }
    void Because() => Plan();

    [Fact] void should_declare_the_stream() => _code.Contains("EventSourceAttribute<", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_existing_handler_shape() => _code.Contains("public global::InvoiceApp.Banking.Deposits.Deposit.Deposited Handle()", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_emit_a_stream_identity() => _code.Contains("EventStreamId =", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_emit_an_unused_codec() => _plan.Artifacts.Any(artifact => artifact.RelativePath == "GeneratedEventSources/StreamIds.cs").ShouldBeFalse();
    [Fact] void should_compile() => RenderedOutput.Errors(Files()).ShouldBeEmpty();
}
