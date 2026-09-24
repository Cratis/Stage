// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_releasing_and_multiple_constraint_targets : given.an_invoice_model
{
    void Because() => Plan(Invoices.Replace(
        "      event InvoiceIssued\n",
        """
              constraint UniqueDescription
                unique description on InvoiceIssued
                unique description on InvoiceImported
                released by InvoiceReleased
                ignore casing
              event InvoiceIssued
        """ + "\n",
        StringComparison.Ordinal) + """

            slice StateChange Import
              command ImportInvoice
                streamReference String identifier
                description String
                produces InvoiceImported
                  for streamReference
                  description = description
              event InvoiceImported
                description String
            slice StateChange Release
              command ReleaseInvoice
                streamReference String identifier
                description String
                produces InvoiceReleased
                  for streamReference
                  description = description
              event InvoiceReleased
                description String
        """);

    [Fact] void should_plan_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_each_target_and_release() => Artifact("UniqueDescription.cs")
        .ShouldContain(".On<global::Invoices.Billing.Invoicing.Import.InvoiceImported>");
    [Fact] void should_render_release() => Artifact("UniqueDescription.cs").ShouldContain(".RemovedWith<global::Invoices.Billing.Invoicing.Release.InvoiceReleased>()");
    [Fact] void should_ignore_casing() => Artifact("UniqueDescription.cs").ShouldContain(".IgnoreCasing()");
}
