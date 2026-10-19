// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticLiveQuery.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticLiveQuery;

public class when_reading_the_current_value : a_live_query
{
    string _beforeChange = null!;
    string _afterChange = null!;

    async Task Because()
    {
        var observer = new a_recording_observer();
        _query.Subscribe(observer);
        await observer.Next();
        _beforeChange = _query.Value;
        _read = () => Task.FromResult("changed");
        _world.Raise();
        await observer.Next();
        _afterChange = _query.Value;
    }

    [Fact] void should_answer_the_initial_value_before_a_change() => _beforeChange.ShouldEqual("initial");
    [Fact] void should_answer_the_latest_emitted_value() => _afterChange.ShouldEqual("changed");
}
