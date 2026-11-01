// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_generated_local_collisions : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = compiled_plan.From("""
        concept Receipt : Uuid
        module Records
          feature Recording
            slice StateChange Record
              command RecordReceipt
                id String identifier
                operation Receipt generated
                occurred Receipt generated
                stageEvents Receipt generated
                stageProduces0 Receipt generated
                streamIdPart0 Receipt generated
                source Receipt generated
                enabled Bool
                produces when enabled == true
                  Recorded
                    for id
                    receipt = operation
                    recordedAt = $context.occurred
              event Recorded
                receipt Receipt
                recordedAt DateTime
        """);

    void Because() => _admission = new(_plan);

    [Fact] void should_admit_names_that_only_clash_in_the_cratis_handle() => _admission.Blocking.ShouldBeEmpty();
}
