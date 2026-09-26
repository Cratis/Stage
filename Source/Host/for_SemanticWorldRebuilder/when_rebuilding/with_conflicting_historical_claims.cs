// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_conflicting_historical_claims : a_rebuildable_world
{
    Exception? _error;

    void Because()
    {
        var second = new AppendedEventResponse
        {
            Context = new Cratis.Chronicle.Contracts.Sequences.EventContext
            {
                EventType = _event.Context.EventType,
                EventSourceId = "d7772ed1-59ea-429f-8973-8ef5b8c60470",
                SequenceNumber = 1,
                Occurred = _event.Context.Occurred,
                Tags = _event.Context.Tags
            },
            Content = _event.Content.Replace("3fa85f64-5717-4562-b3fc-2c963f66afa6", "d7772ed1-59ea-429f-8973-8ef5b8c60470", StringComparison.Ordinal)
        };
        _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event, second], 1));
    }

    [Fact] void should_refuse_the_historical_claims() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_name_the_constraint() => _error!.Message.ShouldContain("UniqueProjectName");
}
