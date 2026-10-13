// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_the_display_name_of_a_pinned_event : given.a_pinned_event
{
    Exception? _error;

    void Establish() => _event.Context.EventType.Id = "Renamed";
    void Because() => _error = Catch.Exception(() => SemanticWorldRebuilder.Fact(_plan, _event));

    [Fact] void should_refuse_history_under_the_unpinned_name() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
