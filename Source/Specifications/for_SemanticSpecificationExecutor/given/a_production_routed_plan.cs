// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

public class a_production_routed_plan : a_routed_plan
{
    protected const string Header = """
        eventsource Account
          id "stored-account"
          identifier String
          stream Transactions
            id "stored-transactions"
            streamId String
          stream Archive
            id "stored-archive"
            streamId String
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
                produces Archived
                  for accountId
                  stream Account.Archive
                    streamId = "archive-1"
              event Deposited
              event Archived
        """;

    protected virtual string Then => """
              specification Depositing
                when Deposit
                  accountId = "acc-1"
                  period = "2026-10"
                then Deposited
                  stream Account.Transactions
                    streamId = "2026-10"
                then Archived
                  stream Account.Archive
                    streamId = "archive-1"
        """;

    void Establish()
    {
        _plan = Compile(Header + "\n" + Then);
        _specification = _plan.Specifications.Values.Single();
        _command = _plan.Commands.Values.Single();
    }
}
