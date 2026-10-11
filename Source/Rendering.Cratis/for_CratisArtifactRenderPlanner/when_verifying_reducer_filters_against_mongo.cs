// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Verifies the generated reducer's observer filter against the pinned Chronicle kernel and MongoDB.
/// </summary>
public class when_verifying_reducer_filters_against_mongo : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = when_rendering_reducer_observer_filters.Source("OrdersSource.Entries")
            .Replace("return new Total(context.Event.Id, context.Event.Amount);",
                "return new Total(context.Event.Id, (context.State?.Amount ?? 0m) + context.Event.Amount);", StringComparison.Ordinal);
        return when_rendering_a_pure_reducer.Plan(when_rendering_a_pure_reducer.Load(source).GetAwaiter().GetResult());
    }

#pragma warning disable CRSPEC0004 // MongoFact derives from FactAttribute and is discovered by xUnit.
    [MongoFact]
    async Task should_filter_reducer_appends_when_configured()
#pragma warning restore CRSPEC0004
    {
        try
        {
            AddGeneratedSpecification("ReducerFilterMongoProbe.cs", """
                using Cratis.Chronicle;
                using Cratis.Chronicle.Events;
                using Cratis.Chronicle.Reducers;
                using Cratis.Chronicle.Registrations;
                using Cratis.Serialization;
                using Projects.Orders.Ordering.PlaceOrder;
                using Projects.Orders.Ordering.Totals;
                using Xunit;

                namespace Projects.ReducerFilterMongoProbe;

                public class FilterProbe
                {
                    [Fact]
                    public async Task should_apply_only_matching_routes()
                    {
                        var address = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
                        Assert.False(string.IsNullOrWhiteSpace(address));
                        using var client = new ChronicleClient(ChronicleOptions.FromConnectionString(new Cratis.Chronicle.Connections.ChronicleConnectionString(address!)),
                            namingPolicy: new CamelCaseNamingPolicy());
                        var store = await client.GetEventStore("Stage207ReducerFilters", Guid.NewGuid().ToString("N"));
                        var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(90));
                        Assert.True(registration.IsSuccess, $"Registration failed: {registration.Failure}; {string.Join("; ", registration.Failures)}");
                        var key = Guid.NewGuid();
                        EventSourceId sourceId = key.ToString("D");
                        await Matching(10m);
                        Assert.Equal(10m, (await store.ReadModels.GetInstanceById<Total>(sourceId))!.Amount);

                        var wrongStream = await store.EventLog.Append(sourceId, new OrderPlaced(key, 100m),
                            eventStreamType: "stored-ignored", eventSourceType: "stored-orders");
                        Assert.True(wrongStream.IsSuccess, $"Wrong-stream append failed: {wrongStream}");
                        // A matching zero-valued fact on the same partition is a completion barrier, not a sleep.
                        await Matching(0m);
                        Assert.Equal(10m, (await store.ReadModels.GetInstanceById<Total>(sourceId))!.Amount);

                        var unrouted = await store.EventLog.Append(sourceId, new OrderPlaced(key, 200m));
                        Assert.True(unrouted.IsSuccess, $"Unrouted append failed: {unrouted}");
                        await Matching(0m);
                        Assert.Equal(10m, (await store.ReadModels.GetInstanceById<Total>(sourceId))!.Amount);

                        await Matching(5m);
                        var reduced = await store.ReadModels.GetInstanceById<Total>(sourceId);
                        Assert.NotNull(reduced);
                        Assert.Equal(key, reduced.Id);
                        Assert.Equal(15m, reduced.Amount);
                        Assert.Empty(await store.Reducers.GetFailedPartitionsFor<Fold>());

                        async Task Matching(decimal amount)
                        {
                            var appended = await store.EventLog.Append(sourceId, new OrderPlaced(key, amount),
                                eventStreamType: "stored-entries", eventSourceType: "stored-orders");
                            Assert.True(appended.IsSuccess, $"Matching append failed: {appended}");
                            await store.Reducers.WaitTillReachesEventSequenceNumber<Fold>(appended.SequenceNumber, TimeSpan.FromSeconds(60));
                        }
                    }
                }
                """);
            var build = await Run("reducer-filters-build.log", "build", "Projects.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var test = await Run("reducer-filters-probe.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", "FullyQualifiedName~FilterProbe.should_apply_only_matching_routes", "--nologo");
            Assert.Contains("Passed!", test, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
