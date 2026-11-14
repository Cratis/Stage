// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_reading_models;

public class after_outside_appends : a_runtime_with_outside_appends
{
    ImmutableArray<SemanticReadModelInstance> _instances;

    async Task Because() => _instances = await _runtime.ReadModels(_runtime.Plan.ReadModels.Values.Single().Id);

    [Fact] void should_read_the_outside_projection() => Assert.Equal(SemanticValue.Text("Outside"), Name(Assert.Single(_instances)));
    [Fact] void should_keep_the_session_healthy() => Assert.Null(((ISemanticRuntimeStatus)_runtime).FaultReason);
    [Fact] void should_not_write_any_facts() => Assert.Equal(0, _log.Writes);
}
