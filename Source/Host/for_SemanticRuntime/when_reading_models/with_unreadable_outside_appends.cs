// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_reading_models;

public class with_unreadable_outside_appends : a_runtime_with_outside_appends
{
    ImmutableArray<SemanticReadModelInstance> _instances;

    void Establish() => RefuseOutsideHistory();

    async Task Because() => _instances = await _runtime.ReadModels(_runtime.Plan.ReadModels.Values.Single().Id);

    [Fact] void should_not_return_stale_instances() => Assert.Empty(_instances);
    [Fact] void should_fault_the_session() => Assert.NotNull(((ISemanticRuntimeStatus)_runtime).FaultReason);
    [Fact] void should_report_unsupported_status() => Assert.Equal("unsupported", SemanticHost.Status(_runtime, "Projects", "/nonexistent/1059").State);
}
