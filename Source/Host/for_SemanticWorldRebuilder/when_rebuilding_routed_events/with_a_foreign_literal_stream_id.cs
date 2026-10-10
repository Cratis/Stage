// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class with_a_foreign_literal_stream_id : given.a_routed_event
{
    Exception? _error;

    void Establish() => _plan = compiled_plan.From(a_routed_runtime.Source.Replace("streamId = period", "streamId = \"fixed\"", StringComparison.Ordinal));

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event], 0));

    [Fact] void should_refuse_an_id_no_producer_could_write() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
