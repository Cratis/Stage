// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticLiveQuery.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticLiveQuery;

public class when_the_last_observer_unsubscribes : a_live_query
{
    int _disposalsWithOneLeft;

    void Because()
    {
        var first = _query.Subscribe(new a_recording_observer());
        var second = _query.Subscribe(new a_recording_observer());
        first.Dispose();
        _disposalsWithOneLeft = _world.Disposals;
        second.Dispose();
    }

    [Fact] void should_keep_listening_while_an_observer_remains() => _disposalsWithOneLeft.ShouldEqual(0);
    [Fact] void should_listen_to_the_world_once() => _world.Subscriptions.ShouldEqual(1);
    [Fact] void should_dispose_the_world_subscription() => _world.Disposals.ShouldEqual(1);
}
