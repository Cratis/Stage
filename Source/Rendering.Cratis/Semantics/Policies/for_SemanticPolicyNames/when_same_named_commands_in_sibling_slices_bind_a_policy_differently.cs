// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Policies.for_SemanticPolicyNames;

public class when_same_named_commands_in_sibling_slices_bind_a_policy_differently : given.a_policy_application
{
    const string Slices = """
            slice StateChange IssueInvoice
              command Issue
                authorize Owner
                invoiceId InvoiceId identifier
                produces InvoiceIssued
                  for invoiceId
                  invoiceId = invoiceId
              event InvoiceIssued
                invoiceId InvoiceId
            slice StateChange IssueOrder
              command Issue
                authorize Owner
                orderId InvoiceId identifier
                produces OrderIssued
                  for orderId
                  orderId = orderId
              event OrderIssued
                orderId InvoiceId
        """;

    void Because() => Compile(Module("Billing", Feature("Invoicing", Slices)));

    [Fact] void should_qualify_each_with_its_slice() => (ClassIn("IssueInvoice"), ClassIn("IssueOrder")).ShouldEqual(("OwnerForBillingInvoicingIssueInvoiceIssuePolicy", "OwnerForBillingInvoicingIssueOrderIssuePolicy"));

    string ClassIn(string slice) => policy_names.ClassOf(_model, _model.Application.Modules.Single().Features.Single().Slices.Single(candidate => candidate.Name == slice).Commands.Single().Id);
}
#endif
