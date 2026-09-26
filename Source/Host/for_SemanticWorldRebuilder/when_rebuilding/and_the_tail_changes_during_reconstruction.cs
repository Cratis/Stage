// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class and_the_tail_changes_during_reconstruction : a_stored_sequence
{
    Exception? _error;

    void Establish() => _services.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(call =>
        Task.FromResult(QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new()
        {
            SequenceNumber = call.Arg<TailSequenceNumberRequest>().EventTypeIds is null ? 1UL : 0UL
        })));

    async Task Because() => _error = await Catch.Exception(() => SemanticChronicleRegistration.Rebuild(_accessor, "Projects", _plan, 0));

    [Fact] void should_refuse_the_changed_history() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
    [Fact] void should_name_the_tail_change() => _error!.Message.ShouldContain("tail changed");
}
