// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_refusing_the_arc_default_stream_id : Specification
{
    const string Model = """
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
                produces event Deposited
                  for accountId
                produces event Archived
                  for accountId
                  stream Account.Archive
                    streamId = "Default"
        """;

    IReadOnlyList<(string Code, string Message)> _diagnostics = [];

    void Because() => _diagnostics = [.. invoice_model.Plan(invoice_model.Compile(Model)).Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message))];

    [Fact] void should_refuse_the_production_route() => _diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-036");
    [Fact] void should_explain_the_arc_fallback() => _diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("EventStreamId.Default", StringComparison.Ordinal));
}
