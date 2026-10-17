// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ScreenplayEventModelVisitor;

public class when_refusing_composite_read_model_keys : Specification
{
    const string Source = """
        module Billing
          feature Payments
            slice StateView Totals
              readmodel PaymentTotals
                invoiceId String key
                method String key
                total Int
              query TotalsForInvoice => PaymentTotals[]
                filter invoiceId String
        """;

    InvalidEventModel? _ensured;
    InvalidEventModel? _visited;

    void Because()
    {
        var parsed = new ScreenplayCompiler().Parse(Source);
        Assert.True(parsed.Success, string.Join(';', parsed.Diagnostics));
        _ensured = Assert.Throws<InvalidEventModel>(() => ScreenplayEventModelVisitor.EnsureBehaviorSupported(parsed.Value!));
        _visited = Assert.Throws<InvalidEventModel>(() => new ScreenplayEventModelVisitor().Visit(parsed.Value!));
    }

    [Fact] void should_refuse_with_the_composite_key_code() => _ensured!.Message.ShouldContain("STAGE-ESM-048");
    [Fact] void should_name_the_read_model() => _ensured!.Message.ShouldContain("'PaymentTotals'");
    [Fact] void should_refuse_before_the_legacy_engine_visits_the_model() => _visited!.Message.ShouldContain("STAGE-ESM-048");
}
