// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_an_absence_assertion : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = compiled_plan.From("""
        module Billing
          feature Invoices
            slice StateView InvoiceLookup
              event InvoiceRemoved
                invoiceId String
              readmodel InvoiceView
                invoiceId String
              query InvoiceById => InvoiceView?
                by invoiceId String
              projection Invoices => InvoiceView
                remove with InvoiceRemoved key invoiceId
              specification RemovingAnInvoice
                given readmodel InvoiceView
                  invoiceId = "first"
                when append InvoiceRemoved
                  invoiceId = "first"
                then no readmodel InvoiceView for "first"
        """);

    void Because() => _admission = new(_plan);

    [Fact] void should_compile_an_esm_v5_model() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V5);
    [Fact] void should_not_block_the_model_for_a_live_specification() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_report_the_absence_assertion_precisely() => _admission.Entries.Single(entry => entry.Kind == "specification").Details
        .ShouldEqual("STAGE-ESM-027: Specification 'RemovingAnInvoice' asserts a read model is absent (ESM v5); the generated ReadModelScenario exposes only a materialized record, so Stage does not support absence yet.");
    [Fact] void should_report_the_projection_capability() => _admission.Entries.Single(entry => entry.Kind == "specification").Capability.ShouldEqual("Projection");
}
