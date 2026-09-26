// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>Uses a Billing-derived subset without Billing's deferred UI and handlers.</summary>
public class when_rendering_the_billing_payment_reducer : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var compiled = LoadBilling();
        var plan = when_rendering_a_pure_reducer.Plan(compiled);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-023" && diagnostic.Message == "1 transition bodies analysed.");
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("PaymentSummaryReducer.cs", StringComparison.Ordinal));
        return plan;
    }

    internal static LoadedSemanticModel LoadBilling(bool deleteOnNegative = false)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, ".git")) && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var billing = File.ReadAllText(Path.Combine(directory.FullName, "Samples", "Billing", "Ledger", "Payments", "Payments.play"));
        static string Section(string source, string start, string end) =>
            source[source.IndexOf(start, StringComparison.Ordinal)..source.IndexOf(end, source.IndexOf(start, StringComparison.Ordinal), StringComparison.Ordinal)];
        var source = """
            concept PaymentId : Uuid
            concept InvoiceId : Uuid
            concept Money : Decimal
            module Ledger
              feature Payments
                slice StateChange RecordPayment
                  command RecordPayment
                    paymentId PaymentId identifier
                    invoiceId InvoiceId
                    amount Money
                    settlesInvoice Bool
                    produces PaymentRecorded
                      for paymentId
                      paymentId = paymentId
                      invoiceId = invoiceId
                      amount = amount
                      settlesInvoice = settlesInvoice
            """ + "\n" + Section(billing, "      event PaymentRecorded\n", "\n      constraint OnePaymentPerAttempt") + "\n" + """
                  specification RecordingAPayment
                    when RecordPayment
                      paymentId = "5c2a1b3d-4e5f-6071-8293-a4b5c6d7e8f9"
                      invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                      amount = 500
                      settlesInvoice = true
                    then PaymentRecorded
                      paymentId = "5c2a1b3d-4e5f-6071-8293-a4b5c6d7e8f9"
                      invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                      amount = 500
                      settlesInvoice = true
                    then readmodel PaymentSummary
                      paymentId = "5c2a1b3d-4e5f-6071-8293-a4b5c6d7e8f9"
                      invoiceId = "8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"
                      amount = 500
                slice StateView PaymentList
            """ + "\n" + Section(billing, "      readmodel PaymentSummary\n", "\n      query Payments =>") + "\n";
        if (deleteOnNegative)
        {
            source = source.Replace(
                "return new PaymentSummary(context.Event.PaymentId, context.Event.InvoiceId, context.Event.Amount);",
                "if (context.Event.Amount.Value < 0m) return null; return new PaymentSummary(context.Event.PaymentId, context.Event.InvoiceId, context.Event.Amount);",
                StringComparison.Ordinal);
        }

        return when_rendering_a_pure_reducer.Load(source).GetAwaiter().GetResult();
    }

    [Fact]
    void should_match_the_real_compilation_purity_verdict_for_the_billing_body()
    {
        var compiled = LoadBilling();
        var plan = when_rendering_a_pure_reducer.Plan(compiled);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var descriptor = Assert.Single(compiled.TypedContextDescriptors);
        var requirement = Assert.Single(compiled.ImplementationRequirements.Where(_ => _.Role == SemanticImplementationRole.ReducerTransition));
        var request = new ArtifactRenderRequest(
            compiled.Model,
            compiled.Plan,
            CratisRendering.CreateProfile("Projects", new("Projects", "Projects")),
            new(ArtifactRenderScopeKind.Application, compiled.Model.Application.Id))
        {
            TypedContextDescriptors = compiled.TypedContextDescriptors
        };
        var context = new SemanticApplicationContext(request, new("Projects", "Projects"));
        var reducer = Assert.Single(context.Reducers);
        var transition = Assert.Single(reducer.Transitions);
        var analysis = PureTransitionAdmission.Analyze(
            compiled.ImplementationContents[requirement.RequirementId],
            context,
            context.ReadModels[reducer.ReadModel],
            context.Events[transition.EventContract],
            descriptor,
            requirement);
        var files = plan.Artifacts.Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && _.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var compilation = RenderedOutput.CreateCompilation(files);
        var reducerFile = plan.Artifacts.Single(_ => _.RelativePath.EndsWith("PaymentSummaryReducer.cs", StringComparison.Ordinal));
        var rendered = PureTransitionAdmission.AnalyzeRendered(compilation, reducerFile.RelativePath, descriptor);
        Assert.True(analysis.Accepted && rendered.Accepted, $"analysis: {analysis.Reason}; rendered: {rendered.Reason}; {string.Join("; ", compilation.GetDiagnostics())}");
        Assert.Equal(analysis.ContextReads, rendered.ContextReads);
        Assert.Equal(analysis.UsedAllowlistEntries, rendered.UsedAllowlistEntries);
    }

    [Fact]
    async Task should_compile_and_pass_generated_reducer_specifications()
    {
        try
        {
            var build = await Run("billing-build.log", "build", "Projects.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "--nologo");
            BuildWarnings(build).ShouldEqual(string.Empty);
            var test = await Run("billing-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--nologo");
            Assert.Contains("Passed!", test, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}

#endif
