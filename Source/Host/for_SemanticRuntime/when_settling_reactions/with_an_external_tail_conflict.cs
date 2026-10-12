// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_an_external_tail_conflict : a_reaction_runtime
{
    void Establish() => ((ISemanticFactTail)_appender).Tail().Returns(0UL);

    async Task Because() => _result = await Execute();

    [Fact] void should_refuse_the_whole_settled_batch() => _result.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_append_nothing() => _batches.ShouldBeEmpty();
    [Fact] void should_return_the_original_world() => _result.World.Facts.ShouldBeEmpty();
    [Fact] void should_fault_the_session() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldNotBeNull();
}
