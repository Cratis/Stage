// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>Uses Billing's event and read model with a negative-amount deletion body to exercise Mongo removal.</summary>
public class when_verifying_billing_mongo_deletion : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan() =>
        when_rendering_a_pure_reducer.Plan(when_rendering_the_billing_payment_reducer.LoadBilling(deleteOnNegative: true));

    [MongoFact]
    async Task mongo_container_matches_model_bound_read_model_query_when_configured()
    {
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
                        var removed = await store.EventLog.Append((EventSourceId)key, new PaymentRecorded(key, invoice, new Money(-1m), true));
                        Assert.True(removed.IsSuccess, $"Append failed: {removed}");
                        await store.Reducers.WaitTillReachesEventSequenceNumber<PaymentSummaryReducer>(removed.SequenceNumber, TimeSpan.FromSeconds(60));
                        Assert.Empty(await store.Reducers.GetFailedPartitionsFor<PaymentSummaryReducer>());
                        Assert.Null(await store.ReadModels.GetInstanceById<PaymentSummary>((EventSourceId)key));
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
}

public sealed class MongoFactAttribute : FactAttribute
{
    public override string? Skip
    {
        get => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION"))
            ? "STAGE_CHRONICLE_MONGO_CONNECTION is not configured."
            : null;
        set => base.Skip = value;
    }
}
#endif
