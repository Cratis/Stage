// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_rejected_invocation : a_reaction_runtime
{
    protected override string Model => StartSource + InvokingSource.Replace("amount Decimal\n        produces", "amount Decimal\n        validate\n          amount < 0 message \"Must be negative\"\n        produces", StringComparison.Ordinal);

    async Task Because() => _result = await Execute();

    [Fact] void should_reject_the_whole_command() => _result.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_append_nothing() => _batches.ShouldBeEmpty();
    [Fact] void should_return_the_original_world() => _result.World.Facts.ShouldBeEmpty();
    [Fact] void should_keep_the_session_usable() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldBeNull();
}
