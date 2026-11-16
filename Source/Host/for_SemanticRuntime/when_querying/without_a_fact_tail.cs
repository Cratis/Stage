// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_querying;

public class without_a_fact_tail : a_semantic_runtime
{
    SemanticExecutionResult _result = null!;

    void Establish() => _runtime = SemanticRuntimeHosting.Create(_runtime.Plan, Substitute.For<IAppendSemanticFacts>());

    async Task Because() => _result = await _runtime.Query(_runtime.Plan.Queries.Values.Single(), SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"), new ClaimsPrincipal());

    void Destroy() => ((SemanticRuntime)_runtime).Dispose();

    [Fact] void should_still_accept_queries() => Assert.IsType<SemanticAccepted>(_result);
    [Fact] void should_keep_the_session_healthy() => Assert.Null(((ISemanticRuntimeStatus)_runtime).FaultReason);
}
