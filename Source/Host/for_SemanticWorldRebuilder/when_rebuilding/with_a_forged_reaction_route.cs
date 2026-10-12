// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_forged_reaction_route : given.a_persisted_reaction_world
{
    Exception? _error;

    void Establish()
    {
        _history[1].Context.EventSourceType = "stored-account";
        _history[1].Context.EventStreamType = "stored-transactions";
        _history[1].Context.EventStreamId = "2026-10";
    }

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, _history, 1));

    [Fact] void should_refuse_a_route_settlement_would_not_produce() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
