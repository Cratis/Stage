// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_validate_only : a_reaction_runtime
{
    async Task Because() => _result = await Execute(validateOnly: true);

    [Fact] void should_validate_the_initiating_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_not_settle_reactions() => FactNames(((SemanticAccepted)_result).Facts).ShouldContainOnly(["Started"]);
    [Fact] void should_append_nothing() => _batches.ShouldBeEmpty();
}
