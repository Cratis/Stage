// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class with_a_noncanonical_stream_id : given.a_routed_event
{
    Exception? _error;

    void Establish() => _event.Context.EventStreamId = "private-e\u0301";

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event], 0));

    [Fact] void should_refuse_reconstruction() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_keep_the_diagnostic_value_free() => _error!.Message.ShouldNotContain(_event.Context.EventStreamId);
}
