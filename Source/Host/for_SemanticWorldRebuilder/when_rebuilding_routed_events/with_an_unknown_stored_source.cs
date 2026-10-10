// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class with_an_unknown_stored_source : given.a_routed_event
{
    Exception? _error;

    void Establish() => _event.Context.EventSourceType = "foreign-source";

    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event], 0));

    [Fact] void should_refuse_the_unknown_route() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
