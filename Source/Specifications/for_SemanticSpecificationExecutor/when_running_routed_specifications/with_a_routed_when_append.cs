// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_a_routed_when_append : a_routed_plan
{
    void Establish()
    {
        var source = Source
            .Replace("for accountId", "for accountId\n          value = period", StringComparison.Ordinal)
            .Replace("event Deposited", "event Deposited\n        value String", StringComparison.Ordinal)
            .Replace("streamId = \"2026-10\"", "streamId = \"2026-10\"\n          value = \"2026-10\"", StringComparison.Ordinal);
        _plan = Compile(source + """

                  readmodel Receipt
                    accountId String
                    value String
                  query ReceiptById => Receipt?
                    by accountId String
                  projection ReceiptProjection => Receipt
                    from Deposited
                      value = value
            """);
        _specification = _plan.Specifications.Values.Single();
        _command = _plan.Commands.Values.Single();
    }

    async Task Because()
    {
        var expected = _specification.ThenEvents[0];
        await Run(With(_specification with
        {
            When = null,
            WhenAppended = new(expected.EventContract, expected.Values) { EventSource = new(Text, SemanticValue.Text("acc-1")), Route = expected.Route },
            ThenEvents = [],
            ThenReadModels = [new(_plan.ReadModels.Values.Single().Id, SemanticValue.Text("acc-1"), [new(_plan.ReadModels.Values.Single().Properties.Single(property => property.Name == "accountId").Id, SemanticValue.Text("acc-1"))])]
        }));
    }

    [Fact] void should_pass_like_the_reference() => AssertParity(true);
    [Fact] void should_append_one_fact() => _result.Trace!.Facts.Count.ShouldEqual(1);
}
