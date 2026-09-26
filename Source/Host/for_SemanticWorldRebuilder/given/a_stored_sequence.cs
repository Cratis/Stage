// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using NSubstitute;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_stored_sequence : a_rebuildable_world
{
    protected IChronicleServicesAccessor _accessor = null!;
    protected IServices _services = null!;

    void Establish()
    {
        _accessor = Substitute.For<IChronicleServicesAccessor>();
        _services = Substitute.For<IServices>();
        _accessor.Services.Returns(_services);
        _services.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(
            QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = 0 }));
        _services.Sequences.FromSequenceNumber(Arg.Any<FromSequenceNumberRequest>()).Returns(
            QueryResult<IEnumerable<AppendedEventResponse>>.Success(Guid.Empty, [_event]));
    }
}
