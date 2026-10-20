// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_unsourced_property_removals_observed_by_all : Specification
{
    const string AllObserver = """
            slice StateView AllInvoiceLookup
              event InvoiceRegistered
                description String
              readmodel AllInvoiceView
                invoiceId String key
                lastSeen String?
              projection AllInvoices => AllInvoiceView
                from InvoiceRegistered
                  lastSeen = description
                all
                  lastSeen = $eventSourceId
        """;

    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _sourcedPlan = null!;

    void Because()
    {
        _sourcedPlan = invoice_model.Plan(invoice_model.Compile(when_rendering_property_keyed_removal_only_projections.Source + "\n" + AllObserver));
        var source = when_rendering_property_keyed_removal_only_projections.Source.Replace(
            "        when append InvoiceRemoved\n          for \"unrelated\"", "        when append InvoiceRemoved", StringComparison.Ordinal);
        _plan = invoice_model.Plan(invoice_model.Compile(source + "\n" + AllObserver));
    }

    [Fact] void should_refuse_the_observable_invented_source() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_a_typed_specification_refusal() => Assert.Contains(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-051" && diagnostic.Message.Contains("appended event 'InvoiceRemoved' has no event source", StringComparison.Ordinal));
    [Fact] void should_admit_the_same_observers_with_an_explicit_source() => Assert.True(_sourcedPlan.Success, string.Join(Environment.NewLine, _sourcedPlan.Diagnostics));
    [Fact] void should_not_emit_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
#endif
