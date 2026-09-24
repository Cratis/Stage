// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

using context = Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_portable_constraints.with_a_unique_value.context;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_executing_portable_constraints;

public class with_a_unique_value(context fixture) : IClassFixture<context>
{
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldEqual(string.Empty);
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldEqual(string.Empty);
    [Fact] void should_pass_generated_specs() => fixture.Results.All(_ => _.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_execute_violation_and_nonviolating_cases() => fixture.Results.Length.ShouldEqual(11);

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace(
                "      event InvoiceIssued\n",
                """
                  constraint UniqueDescription
                    unique description on InvoiceIssued
                    message "Description is already in use"
                  event InvoiceIssued
            """ + "\n",
                StringComparison.Ordinal)
            .Replace(
                "      specification IssuingSecondInvoice\n",
                """
                  specification ReusingDescription
                    given InvoiceIssued
                      for "invoice-one"
                      description = "First payload"
                    when IssueInvoice
                      description = "First payload"
                      streamReference = "invoice-two"
                    then error "Description is already in use"
                  specification IssuingSecondInvoice
            """ + "\n",
                StringComparison.Ordinal);

        async Task Because()
        {
            AddGeneratedSpecification("Billing/Invoicing/Issue/when_null_description.cs", """
                // Copyright (c) Cratis. All rights reserved.
                // Licensed under the MIT license. See LICENSE file in the project root for full license information.

                #if DEBUG
                using Cratis.Arc.Chronicle.Testing.Commands;
                using Cratis.Arc.Testing.Commands;
                using Cratis.Specifications;
                using Xunit;

                namespace Invoices.Billing.Invoicing.Issue;

                public class when_null_description : Specification, IDisposable
                {
                    readonly CommandScenario<IssueInvoice> _scenario = new();
                    Cratis.Arc.Commands.CommandResult _result = null!;

                    async Task Because() => _result = await _scenario.Execute(new IssueInvoice(null!, "invoice-three"));

                    [Fact] void should_reject_null_before_append() => _result.ShouldHaveValidationErrors();
                    [Fact] void should_not_append_an_event() => _scenario.AppendedEvents.ShouldBeEmpty();

                    public void Dispose() => _scenario.Dispose();
                }
                #endif
                """);
            await VerifyGeneratedApplication();
        }
    }
}
#endif
