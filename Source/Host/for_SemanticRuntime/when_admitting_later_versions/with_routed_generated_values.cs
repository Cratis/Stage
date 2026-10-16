// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_routed_generated_values : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = compiled_plan.From("""
        concept Receipt : Uuid
          generate uuidv7
        eventsource Account
          identifier Receipt
          stream Transactions
            streamId String
        module Banking
          feature Recording
            slice StateChange Record
              command Record
                receipt Receipt generated identifier
                period String
                stream Account.Transactions
                  streamId = period
                produces event Recorded
                  for receipt
              specification Recording
                when Record
                  for "11111111-1111-7111-8111-111111111111"
                  period = "2026-10"
                then Recorded
                  for "11111111-1111-7111-8111-111111111111"
                  stream Account.Transactions
                    streamId = "2026-10"
        """);

    void Because() => _admission = new(_plan);

    [Fact] void should_admit_generated_destinations_with_request_stream_ids() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_admit_routed_generated_specification_execution() => SemanticRunAdmission.Check(_plan, _plan.Specifications.Values.Single()).ShouldBeNull();
}
