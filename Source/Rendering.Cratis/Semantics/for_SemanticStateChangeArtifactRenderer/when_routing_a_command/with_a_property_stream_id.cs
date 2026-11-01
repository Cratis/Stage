// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_a_property_stream_id : given.a_routed_command
{
    void Because() => Plan();

    [Fact] void should_declare_the_source_and_stream() => _code.Contains("EventSource<AccountEventSource>(AccountEventSource.Transactions)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_validate_the_integer_inside_the_handler() => _code.Contains("TryInteger(Month.Value, out var streamIdPart0)", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_route_the_validated_integer() => _code.Contains("EventStreamId = streamIdPart0", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_resolve_the_stream_id_before_authorization() => _code.Contains("ICanProvideEventStreamId", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_return_validation_explicitly() => _code.Contains(".FromT1(ValidationResult.Error", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_compile() => RenderedOutput.Errors(Files()).ShouldBeEmpty();
}
