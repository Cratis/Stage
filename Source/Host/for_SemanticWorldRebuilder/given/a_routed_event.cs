// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_routed_event : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected AppendedEventResponse _event = null!;

    void Establish()
    {
        _plan = compiled_plan.From(a_routed_runtime.Source);
        _event = new()
        {
            Context = new()
            {
                EventType = new() { Id = "Deposited", Generation = 1 },
                EventSourceId = "acc-1",
                SequenceNumber = 0,
                EventSourceType = "stored-account",
                EventStreamType = "stored-transactions",
                EventStreamId = "2026-10",
                Occurred = new() { Value = "2026-10-01T12:00:00.0000000+00:00" }
            },
            Content = """{"amount":10}"""
        };
    }
}
