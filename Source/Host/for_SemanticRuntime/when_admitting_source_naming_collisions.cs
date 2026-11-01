// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_admitting_source_naming_collisions : Specification
{
    [Theory]
    [InlineData("Text", "ById")]
    [InlineData("Record", "Total")]
    public void should_admit_declarations_and_their_specifications(string commandName, string queryName)
    {
        var plan = compiled_plan.From($$"""
            module Payments
              feature Recording
                slice StateChange RecordPayment
                  command {{commandName}}
                    id String identifier
                    text String
                    produces Recorded
                      for id
                      text = text
                  event Recorded
                    text String
                  specification Recording
                    when {{commandName}}
                      id = "payment-1"
                      text = "paid"
                    then Recorded
                      text = "paid"
                    then readmodel PaymentTotals
                      id = "payment-1"
                      total = "paid"
                slice StateView Lookup
                  readmodel PaymentTotals
                    id String key
                    total String
                  query {{queryName}} => PaymentTotals?
                    by id String
                  projection Build => PaymentTotals
                    from Recorded
                      id = $eventSourceId
                      total = text
            """);

        var admission = new SemanticRuntimeAdmission(plan);

        admission.Blocking.ShouldBeEmpty();
        Assert.All(admission.Entries, entry => Assert.True(entry.Status == "supported", $"{entry.Kind}: {entry.Details}"));
    }
}
