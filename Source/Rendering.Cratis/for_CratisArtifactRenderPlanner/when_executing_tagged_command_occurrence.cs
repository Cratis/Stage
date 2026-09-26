// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Verifies command metadata reaches Chronicle's append, rather than only a generated source string.
/// </summary>
public class when_executing_tagged_command_occurrence : a_generated_application
{
    const string Probe = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Chronicle.EventSequences;
        using Invoices.Billing.Invoicing.Issue;
        using Xunit;

        namespace Invoices.ProducedEventProbe;

        public class when_appending_a_tagged_occurrence
        {
            static readonly string[] _tags = ["contract", "produced"];

            [Fact]
            public async Task should_preserve_tags_and_payload_occurrence()
            {
                using var scenario = new CommandScenario<IssueInvoice>();
                await scenario.Execute(new IssueInvoice("First", "invoice-one"));
                var appended = Assert.Single(scenario.AppendedEvents);
                Assert.True(appended.Result.IsSuccess);
                var stored = Assert.Single(await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                Assert.Equal(_tags, stored.Context.Tags.Select(tag => tag.Value));
                var payload = Assert.IsType<InvoiceIssued>(stored.Content);
                Assert.Equal(stored.Context.Occurred, payload.IssuedAt);
            }
        }
        #endif
        """;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource);
        source = source[..source.IndexOf("      specification IssuingFirstInvoice", StringComparison.Ordinal)]
            .Replace("          for streamReference\n", "          for streamReference\n          tag \"produced\"\n", StringComparison.Ordinal)
            .Replace("          description = description\n", "          description = description\n          issuedAt = $context.occurred\n", StringComparison.Ordinal)
            .Replace("      event InvoiceIssued\n", "      event InvoiceIssued\n        tag \"contract\"\n        issuedAt DateTime\n", StringComparison.Ordinal);
        var plan = invoice_model.Plan(invoice_model.Compile(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        return plan;
    }

    string _debug = null!;
    string _release = null!;
    string _tests = null!;

    async Task Because()
    {
        AddGeneratedSpecification("when_appending_a_tagged_occurrence.cs", Probe);
        _debug = await Run("metadata-debug.log", "build", "-c", "Debug", "-warnaserror");
        _release = await Run("metadata-release.log", "build", "-c", "Release", "-warnaserror");
        _tests = await Run("metadata-tests.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_build_and_execute_tagged_command_occurrence()
    {
        BuildWarnings(_debug).ShouldEqual(string.Empty);
        BuildWarnings(_release).ShouldEqual(string.Empty);
        _tests.ShouldContain("Passed!");
    }
}
#endif
