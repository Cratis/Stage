// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Stage.Semantics;
using NSubstitute;

using ObserverInformation = Cratis.Chronicle.Contracts.Observation.ObserverInformation;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_registered_mirror : a_rebuildable_world
{
    protected IChronicleServicesAccessor _accessor = null!;
    protected IServices _services = null!;
    protected string _mirrorId = null!;

    void Establish()
    {
        _accessor = Substitute.For<IChronicleServicesAccessor>();
        _services = Substitute.For<IServices>();
        _accessor.Services.Returns(_services);
        SemanticProjectionMirrors.TryLower(_plan, _plan.Projections.Values.Single(), out _, out var projection, out _);
        _mirrorId = projection!.Identifier;
        _services.Observers.GetObservers(Arg.Any<AllObserversRequest>()).Returns([new ObserverInformation
        {
            Id = _mirrorId,
            EventSequenceId = EventSequenceId.Log
        }]);
        _services.Observers.GetObserverInformation(Arg.Any<GetObserverInformationRequest>()).Returns(new ObserverInformation
        {
            Id = _mirrorId,
            IsSubscribed = true,
            RunningState = Cratis.Chronicle.Contracts.Observation.ObserverRunningState.Active,
            LastHandledEventSequenceNumber = 0
        });
        _services.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(
            QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = 0 }));
        _services.Sequences.FromSequenceNumber(Arg.Any<FromSequenceNumberRequest>()).Returns(
            QueryResult<IEnumerable<AppendedEventResponse>>.Success(Guid.Empty, [_event]));
        _services.FailedPartitions.GetFailedPartitions(Arg.Any<GetFailedPartitionsRequest>()).Returns([]);
        _services.ReadModels.GetInstances(Arg.Any<GetInstancesRequest>()).Returns(new GetInstancesResponse
        {
            TotalCount = 1,
            Instances = [_mirror.Values.Single().Single()]
        });
    }
}
