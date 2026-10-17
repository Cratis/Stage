// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_guard_loss_after_an_invocation : a_reaction_runtime
{
    a_semantic_fact_log _log = null!;
    protected override string Model => StartSource + InvokingSource;

    void Establish()
    {
        _log = new();
        _runtime = _log.Runtime(_plan, SemanticWorld.Empty);
        _log.BeforeAppend = () =>
        {
            var destination = SemanticValue.Text("outside");
            _log.Facts.Add(_log.Attempts.Single().Facts[0] with
            {
                Destination = destination,
                Context = new(new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), destination))
            });
        };
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_return_a_decision_state_conflict() => ((SemanticConflict)_result).Category.ShouldEqual(SemanticConflictCategory.DecisionStateChanged);
    [Fact] void should_not_retry_invoked_commands() => _log.Attempts.Count.ShouldEqual(1);
    [Fact] void should_guard_before_any_root_or_cascade_fact() => _log.Attempts.Single().Tail.ShouldEqual(ulong.MaxValue);
    [Fact] void should_submit_the_complete_settled_batch() => FactNames(_log.Attempts.Single().Facts).ShouldEqual(["Started", "Completed"]);
    [Fact] void should_not_persist_any_of_the_tentative_batch() => _log.Writes.ShouldEqual(0);
    [Fact] void should_not_fault_the_session() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldBeNull();
}
