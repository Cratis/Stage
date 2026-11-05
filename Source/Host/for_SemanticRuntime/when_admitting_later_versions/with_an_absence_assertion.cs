// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_an_absence_assertion : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = compiled_plan.From("""
        module Billing
          feature Invoices
            slice StateChange Registration
              command RegisterInvoice
                invoiceId String identifier
                description String
                produces InvoiceRegistered
                  for invoiceId
                  description = description
              event InvoiceRegistered
                description String
            slice StateView InvoiceLookup
              readmodel InvoiceView
                invoiceId String
                description String
              query InvoiceById => InvoiceView optional
                by invoiceId String
              projection Invoices => InvoiceView
                from InvoiceRegistered
                  invoiceId = $eventSourceId
                  description = description
              specification AnUnregisteredInvoiceIsAbsent
                then no readmodel InvoiceView for "first"
        """);

    void Because() => _admission = new(_plan);

    [Fact] void should_compile_an_esm_v5_model() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V5);
    [Fact] void should_not_block_the_model_for_a_live_specification() => _admission.Blocking.ShouldBeEmpty();
    [Fact] void should_admit_the_absence_assertion() => _admission.Entries.Single(entry => entry.Kind == "specification").Status.ShouldEqual("supported");
    [Fact] void should_not_report_a_refusal() => _admission.Entries.Single(entry => entry.Kind == "specification").Capability.ShouldBeNull();
}
