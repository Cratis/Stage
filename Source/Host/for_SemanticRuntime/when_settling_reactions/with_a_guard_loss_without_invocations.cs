// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_guard_loss_without_invocations : a_reaction_runtime
{
    a_semantic_fact_log _log = null!;

    void Establish()
    {
        _log = new();
        _runtime = _log.Runtime(_plan, SemanticWorld.Empty);
        _log.BeforeAppend = () =>
        {
            _log.BeforeAppend = null;
            var destination = SemanticValue.Text("outside");
            _log.Facts.Add(_log.Attempts.Single().Facts[0] with
            {
                Destination = destination,
                Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), destination))
            });
        };
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_retry_a_definitively_unwritten_batch() => _log.Attempts.Count.ShouldEqual(2);
    [Fact] void should_guard_the_new_durable_tail_not_the_tentative_batch() => _log.Attempts[^1].Tail.ShouldEqual(0UL);
    [Fact] void should_settle_only_the_root_batch_not_the_outside_fact() => FactNames(((SemanticAccepted)_result).Facts).ShouldEqual(["Started", "Completed"]);
    [Fact] void should_preserve_the_outside_fact_in_the_accepted_world() => FactNames(_result.World.Facts).ShouldEqual(["Started", "Started", "Completed"]);
    [Fact] void should_write_once() => _log.Writes.ShouldEqual(1);
    [Fact] void should_not_fault_the_session() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldBeNull();
}
