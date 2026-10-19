// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticLiveQuery.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticLiveQuery;

public class when_a_read_is_rejected : a_live_query
{
    a_recording_observer _observer = null!;
    Exception? _error;
    Exception? _lateError;

    void Establish() => _read = () => throw new SemanticQueryRejected("The caller may no longer read this.");

    async Task Because()
    {
        _observer = new();
        _query.Subscribe(_observer);
        _error = await _observer.Ended();
        var late = new a_recording_observer();
        _query.Subscribe(late);
        _lateError = await late.Ended();
    }

    [Fact] void should_end_the_stream_with_the_rejection() => _error.ShouldBeOfExactType<SemanticQueryRejected>();
    [Fact] void should_end_a_later_subscription_the_same_way() => _lateError.ShouldBeOfExactType<SemanticQueryRejected>();
    [Fact] void should_stop_listening_to_the_world() => _world.Disposals.ShouldEqual(1);
}
