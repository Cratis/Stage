// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_settling_reactions;

public class with_a_reached_reducer_body : a_reaction_runtime
{
    protected override string Model => StartSource + ProducingSource + """

              readmodel Snapshot
                id String key
              reducer Fold => Snapshot
                on Completed
                  ```csharp
                  throw new InvalidOperationException("Never execute an authored body");
                  ```
        """;

    async Task Because() => _result = await Execute();

    [Fact] void should_admit_the_model_until_the_body_is_reached() => new SemanticRuntimeAdmission(_plan).Blocking.ShouldBeEmpty();
    [Fact] void should_refuse_the_reached_projection() => ((SemanticUnsupported)_result).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
    [Fact] void should_append_neither_the_command_nor_its_reaction() => _batches.ShouldBeEmpty();
    [Fact] void should_preserve_the_original_world() => _result.World.Facts.ShouldBeEmpty();
    [Fact] void should_not_poison_the_runtime() => ((ISemanticRuntimeStatus)_runtime).FaultReason.ShouldBeNull();
}
