// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticLiveQuery.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticLiveQuery;

public class when_a_read_throws : a_live_query
{
    readonly Exception _failure = new("The read failed.");
    a_recording_observer _first = null!;
    a_recording_observer _second = null!;
    Exception? _firstError;
    Exception? _secondError;

    void Establish() => _read = () => throw _failure;

    async Task Because()
    {
        _first = new();
        _second = new();
        _query.Subscribe(_first);
        _query.Subscribe(_second);
        _world.Raise();
        _firstError = await _first.Ended();
        _secondError = await _second.Ended();
    }

    [Fact] void should_fail_the_first_observer() => _firstError.ShouldBeSame(_failure);
    [Fact] void should_fail_the_second_observer() => _secondError.ShouldBeSame(_failure);
    [Fact] void should_stop_listening_to_the_world() => _world.Disposals.ShouldEqual(1);
}
