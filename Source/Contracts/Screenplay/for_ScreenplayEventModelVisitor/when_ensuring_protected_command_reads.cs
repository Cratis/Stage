// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Screenplay.for_ScreenplayEventModelVisitor;

public class when_ensuring_protected_command_reads : Specification
{
    const string Source = """
        command reads protected
        module Billing
          feature Payments
            slice StateView Totals
              readmodel PaymentTotals
                invoiceId String key
                total Int
              query TotalsForInvoice => PaymentTotals[]
                filter invoiceId String
        """;

    const string CompositeSource = """
        command reads protected
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
    EventModel _visited = null!;
    InvalidEventModel? _composite;

    void Because()
    {
        var parsed = new ScreenplayCompiler().Parse(Source);
        Assert.True(parsed.Success, string.Join(';', parsed.Diagnostics));
        _ensured = Assert.Throws<InvalidEventModel>(() => ScreenplayEventModelVisitor.EnsureBehaviorSupported(parsed.Value!));
        _visited = new ScreenplayEventModelVisitor().Visit(parsed.Value!);
        var composite = new ScreenplayCompiler().Parse(CompositeSource);
        Assert.True(composite.Success, string.Join(';', composite.Diagnostics));
        _composite = Assert.Throws<InvalidEventModel>(() => new ScreenplayEventModelVisitor().Visit(composite.Value!));
    }

    [Fact] void should_keep_refusing_for_the_legacy_renderer() => _ensured!.Message.ShouldContain("STAGE-ESM-044");
    [Fact] void should_admit_the_visit() => _visited.ShouldNotBeNull();
    [Fact] void should_record_the_protected_mode() => _visited.ProtectedCommandReads.ShouldBeTrue();
    [Fact] void should_not_bypass_the_other_refusals() => _composite!.Message.ShouldContain("STAGE-ESM-048");
}
