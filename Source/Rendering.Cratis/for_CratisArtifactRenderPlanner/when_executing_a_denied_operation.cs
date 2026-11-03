// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_denying_an_unsupported_validation_rule.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_executing_a_denied_operation(when_executing_a_denied_operation.context fixture) : IClassFixture<when_executing_a_denied_operation.context>
{
    [Fact] void should_reject_the_command_before_appending() => fixture.Results.Any(result => result.Matches(methodName: "should_reject_before_appending") && result.Outcome == "Passed").ShouldBeTrue();
    [Fact] void should_build_debug_without_warnings() => fixture.DebugWarnings.ShouldBeEmpty();
    [Fact] void should_build_release_without_warnings() => fixture.ReleaseWarnings.ShouldBeEmpty();

    public class context : a_generated_invoice_application
    {
        protected override string InvoiceSource => invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource);

        protected override ArtifactRenderPlan CreatePlan() => an_invoice_command_with_an_unsupported_validation_rule.PlanModel(an_invoice_command_with_an_unsupported_validation_rule.CreateModel());

        async Task Because()
        {
            AddGeneratedSpecification("DenialProbe.cs", """
                #if DEBUG
                using Cratis.Arc.Chronicle.Testing.Commands;
                using Cratis.Arc.Testing.Commands;
                using Invoices.Billing.Invoicing.Issue;
                using Xunit;

                namespace Invoices.Probes;

                public class when_invoking_a_denied_command
                {
                    [Fact]
                    public async Task should_reject_before_appending()
                    {
                        using var command = new CommandScenario<IssueInvoice>();
                        var result = await command.Execute(new IssueInvoice("First payload", "invoice-one"));
                        Assert.False(result.HasExceptions, string.Join("; ", result.ExceptionMessages));
                        Assert.False(result.IsSuccess);
                        Assert.Contains(result.ValidationResults, validation => validation.Message.Contains("STAGE-DEGRADE-002", StringComparison.Ordinal));
                        Assert.Empty(command.AppendedEvents);
                    }
                }
                #endif
                """);
            await VerifyGeneratedApplication();
        }
    }
}
#endif
