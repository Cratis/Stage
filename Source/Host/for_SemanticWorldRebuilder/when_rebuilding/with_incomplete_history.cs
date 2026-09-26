// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_incomplete_history : a_rebuildable_world
{
    [Fact] void should_refuse_an_empty_page_with_a_nonempty_tail() =>
        Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [], 0)).ShouldBeOfExactType<SemanticWorldRebuildRefused>();

    [Fact] void should_refuse_a_tail_beyond_the_last_event() =>
        Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event], 1)).ShouldBeOfExactType<SemanticWorldRebuildRefused>();

    [Fact] void should_refuse_a_duplicate_sequence_number() =>
        Catch.Exception(() => SemanticWorldRebuilder.Create(_plan, [_event, _event], 0)).ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
