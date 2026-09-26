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

/// <summary>Uses the Billing event, read model and body verbatim in a model without Billing's deferred UI and handlers.</summary>
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

    static LoadedSemanticModel LoadBilling()
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
    async Task should_share_the_mongo_container_with_the_model_bound_read_model_query_when_configured()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION"))) return;
        try
        {
            AddGeneratedSpecification("BillingMongoProbe.cs", """
                using Cratis.Chronicle;
                using Cratis.Chronicle.Events;
                using Cratis.Chronicle.Reducers;
                using Cratis.Chronicle.Registrations;
                using Cratis.Serialization;
                using Projects.Common;
                using Projects.Ledger.Payments.PaymentList;
                using Projects.Ledger.Payments.RecordPayment;
                using Xunit;

                namespace Projects.BillingMongoProbe;

                public class when_reading_a_reduced_payment_from_mongo
                {
                    [Fact]
                    public async Task should_use_the_query_read_model_container()
                    {
                        var address = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
                        Assert.False(string.IsNullOrWhiteSpace(address));
                        using var client = new ChronicleClient(ChronicleOptions.FromConnectionString(new Cratis.Chronicle.Connections.ChronicleConnectionString(address!)),
                            namingPolicy: new CamelCaseNamingPolicy());
                        var store = await client.GetEventStore("Stage119", Guid.NewGuid().ToString("N"));
                        var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(90));
                        Assert.True(registration.IsSuccess, $"Registration failed: {registration.Failure}; {string.Join("; ", registration.Failures)}");
                        var key = new PaymentId(Guid.Parse("5c2a1b3d-4e5f-6071-8293-a4b5c6d7e8f9"));
                        var invoice = new InvoiceId(Guid.Parse("8f14e45f-ceea-467a-9c2b-1b7f2ec2a1c1"));
                        var handler = store.Reducers.GetHandlerFor<PaymentSummaryReducer>();
                        // ReadModels.Register uses this same naming policy for the collection queried by PaymentById.
                        Assert.Equal(new CamelCaseNamingPolicy().GetReadModelName(typeof(PaymentSummary)), handler.ContainerName.Value);
                        var appended = await store.EventLog.Append((EventSourceId)key, new PaymentRecorded(key, invoice, new Money(500m), true));
                        Assert.True(appended.IsSuccess, $"Append failed: {appended}");
                        await store.Reducers.WaitTillReachesEventSequenceNumber<PaymentSummaryReducer>(appended.SequenceNumber, TimeSpan.FromSeconds(60));
                        Assert.Empty(await store.Reducers.GetFailedPartitionsFor<PaymentSummaryReducer>());
                        var fromCollection = await store.ReadModels.GetInstanceById<PaymentSummary>((EventSourceId)key);
                        Assert.NotNull(fromCollection);
                        Assert.Equal(key, fromCollection.PaymentId);
                        Assert.Equal(invoice, fromCollection.InvoiceId);
                        Assert.Equal(new Money(500m), fromCollection.Amount);
                    }
                }
                """);
            var build = await Run("billing-mongo-build.log", "build", "Projects.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "--nologo");
            BuildWarnings(build).ShouldEqual(string.Empty);
            var test = await Run("billing-mongo-test.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", "FullyQualifiedName~when_reading_a_reduced_payment_from_mongo", "--nologo");
            Assert.Contains("Passed!", test, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
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
