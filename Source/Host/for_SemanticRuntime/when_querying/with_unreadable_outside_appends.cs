// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_querying;

public class with_unreadable_outside_appends : a_runtime_with_outside_appends
{
    SemanticExecutionResult _result = null!;
    StageStatus _status = null!;

    void Establish() => RefuseOutsideHistory();

    async Task Because()
    {
        _result = await _runtime.Query(_query, ProjectKey, new ClaimsPrincipal());
        _status = SemanticHost.Status(_runtime, "Projects", "/nonexistent/1059");
    }

    [Fact] void should_refuse_instead_of_returning_empty_success() => Assert.IsType<SemanticUnsupported>(_result);
    [Fact] void should_fault_the_session() => Assert.NotNull(((ISemanticRuntimeStatus)_runtime).FaultReason);
    [Fact] void should_report_unsupported_status() => Assert.Equal("unsupported", _status.State);
    [Fact] void should_report_the_fault_reason() => Assert.Equal(((ISemanticRuntimeStatus)_runtime).FaultReason, Assert.Single(_status.Issues!).Details);
    [Fact] void should_not_adopt_partial_history() => Assert.Empty(_result.World.Facts);
}
