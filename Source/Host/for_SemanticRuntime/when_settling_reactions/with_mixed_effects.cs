// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_mixed_effects : a_reaction_runtime
{
    protected override string Model => StartSource + InvokingSource.Replace("          invokes CompleteWork", "          produces Noted\n          invokes CompleteWork", StringComparison.Ordinal) + "\n      event Noted";

    async Task Because() => _result = await Execute();

    [Fact] void should_admit_the_live_reference_effects() => new SemanticRuntimeAdmission(_plan).Blocking.ShouldBeEmpty();
    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_productions_before_invocations() => FactNames(_batches.Single()).ToArray().ShouldEqual(["Started", "Noted", "Completed"]);
}
