// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_the_route_cannot_be_formatted : a_routed_runtime
{
    SemanticExecutionResult _result = null!;

    void Establish() => _period = "private-e\u0301";

    async Task Because() => _result = await Execute();

    [Fact] void should_reject_the_contract() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
    [Fact] void should_keep_the_diagnostic_value_free() => ((SemanticRejected)_result).Details.ShouldNotContain(_period);
    [Fact] void should_append_nothing() => _append.ShouldBeNull();
    [Fact] void should_commit_no_world() => _result.World.Facts.ShouldBeEmpty();
}
