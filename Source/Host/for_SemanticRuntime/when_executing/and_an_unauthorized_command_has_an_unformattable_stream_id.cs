// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_an_unauthorized_command_has_an_unformattable_stream_id : a_routed_runtime
{
    SemanticExecutionResult _result = null!;

    void Establish()
    {
        _principal = new ClaimsPrincipal();
        _period = "private-e\u0301";
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_deny_before_formatting_the_route() => ((SemanticRejected)_result).Category.ShouldEqual(SemanticRejectionCategory.Unauthorized);
    [Fact] void should_append_nothing() => _append.ShouldBeNull();
}
