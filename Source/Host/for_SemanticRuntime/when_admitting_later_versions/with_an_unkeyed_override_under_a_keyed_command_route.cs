// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_an_unkeyed_override_under_a_keyed_command_route : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = compiled_plan.From("""
        concept AccountId : String
        concept Period : String
        eventsource Account
          id "stored-account"
          identifier AccountId
          stream Transactions
            id "stored-transactions"
            streamId Period
          stream Audit
            id "stored-audit"
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId AccountId identifier
                period Period
                stream Account.Transactions
                  streamId = period
                produces Deposited
                  for accountId
                produces Audited
                  for accountId
                  stream Account.Audit
              event Deposited
              event Audited
        """);

    void Because() => _admission = new(_plan);

    [Fact] void should_refuse_the_command() => _admission.Blocking.Where(entry => entry.Details!.StartsWith("STAGE-ESM-036:", StringComparison.Ordinal)).Select(entry => entry.Artifact).ShouldContainOnly(_plan.Commands.Keys.Select(id => id.ToString()));
}
