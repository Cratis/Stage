// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_reaction_only_event : given.a_persisted_reaction_world
{
    void Because() => _rebuilt = SemanticWorldRebuilder.Create(_plan, _history, 1);

    [Fact] void should_restore_the_durable_live_facts() => DurableFacts(_rebuilt).ShouldEqual(DurableFacts(_live));
    [Fact] void should_restore_the_projected_live_world() => ReadModels(_rebuilt).ShouldEqual(ReadModels(_live));
    [Fact] void should_not_react_to_the_history_again() => _rebuilt.Facts.Length.ShouldEqual(2);
    [Fact] void should_not_append_again_during_rebuild() => _contracts.Sequences.Received(1).AppendManyForEventSources(Arg.Any<AppendManyForEventSourcesRequest>());
}
