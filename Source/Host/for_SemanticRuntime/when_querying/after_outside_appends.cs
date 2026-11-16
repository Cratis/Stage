// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_querying;

public class after_outside_appends : a_runtime_with_outside_appends
{
    SemanticExecutionResult _result = null!;

    async Task Because() => _result = await _runtime.Query(_query, ProjectKey, new ClaimsPrincipal());

    [Fact] void should_read_the_outside_projection() => Assert.Equal(SemanticValue.Text("Outside"), Name(Assert.Single(Assert.Single(Assert.IsType<SemanticAccepted>(_result).Queries).Results)));
    [Fact] void should_include_the_outside_fact() => Assert.Single(_result.World.Facts);
    [Fact] void should_keep_the_session_healthy() => Assert.Null(((ISemanticRuntimeStatus)_runtime).FaultReason);
    [Fact] void should_not_write_any_facts() => Assert.Equal(0, _log.Writes);
}
