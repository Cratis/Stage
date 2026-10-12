// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_reactions : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = compiled_plan.From("""
        concept InvoiceId : Uuid
        module Collections
          feature Invoices
            slice StateChange SendInvoice
              command SendInvoice
                invoiceId InvoiceId identifier
                produces InvoiceSent
                  for invoiceId
                  invoiceId = invoiceId
              event InvoiceSent
                invoiceId InvoiceId
            slice Automation Reminders
              reaction ReminderScheduler
                when InvoiceSent
                  invoiceId
                  produces ReminderScheduled
                    scheduledAt = $context.occurred
              event ReminderScheduled
                scheduledAt DateTime
        """);

    void Because() => _admission = new(_plan);

    [Fact] void should_compile_an_esm_v6_model() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_admit_the_automation_slice_and_its_reaction() => _admission.Blocking.ShouldBeEmpty();
}
