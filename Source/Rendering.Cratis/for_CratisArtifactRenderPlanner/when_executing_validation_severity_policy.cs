// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Checks that modeled non-error validation failures block Arc's real command pipeline.
/// </summary>
public class when_executing_validation_severity_policy : a_generated_application
{
    const string Probe = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Validation;
        using Invoices.Billing.Invoicing.Issue;
        using Xunit;

        namespace Invoices.ValidationSeverityProbe;

        public class when_rejecting_non_error_validation
        {
            [Theory]
            [InlineData("", ValidationResultSeverity.Warning)]
            [InlineData("draft", ValidationResultSeverity.Information)]
            public async Task should_reject_without_appending(string description, ValidationResultSeverity severity)
            {
                using var scenario = new CommandScenario<IssueInvoice>();
                var result = await scenario.Execute(new IssueInvoice(description, "invoice-one"));
                Assert.Contains(result.ValidationResults, failure => failure.Severity == severity);
                Assert.Empty(scenario.AppendedEvents);
            }
        }
        #endif
        """;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource)
            .Replace("        produces InvoiceIssued\n", "        validate\n          description not empty severity warning\n          require description != \"draft\"\n            severity information\n        produces InvoiceIssued\n", StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    string _debug = null!;
    string _release = null!;
    string _tests = null!;

    async Task Because()
    {
        AddGeneratedSpecification("when_rejecting_non_error_validation.cs", Probe);
        _debug = await Run("severity-debug.log", "build", "-c", "Debug", "-warnaserror", "--no-cache");
        _release = await Run("severity-release.log", "build", "-c", "Release", "-warnaserror");
        _tests = await Run("severity-test.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_build_and_reject_warning_and_information_without_appending()
    {
        BuildWarnings(_debug).ShouldEqual(string.Empty);
        BuildWarnings(_release).ShouldEqual(string.Empty);
        _tests.ShouldContain("Passed!");
    }
}
#endif
