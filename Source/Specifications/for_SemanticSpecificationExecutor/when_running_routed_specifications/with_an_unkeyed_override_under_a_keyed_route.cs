// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_an_unkeyed_override_under_a_keyed_route : a_production_routed_plan
{
    const string Unkeyed = """
        eventsource Account
          id "stored-account"
          identifier String
          stream Transactions
            id "stored-transactions"
            streamId String
          stream Audit
            id "stored-audit"
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId String identifier
                period String
                stream Account.Transactions
                  streamId = period
                produces Deposited
                  for accountId
                produces Audited
                  for accountId
                  stream Account.Audit
              event Deposited
              event Audited
              specification Depositing
                when Deposit
                  accountId = "acc-1"
                  period = "2026-10"
                then Deposited
        """;

    async Task Because() => await Run(Compile(Unkeyed));

    [Fact] void should_refuse_as_unsupported() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_report_the_production_route_code() => _result.Unsupported!.Details.StartsWith("STAGE-ESM-036:", StringComparison.Ordinal).ShouldBeTrue();
}
