// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Verifies against the pinned Chronicle kernel and MongoDB that a migrated read model keeps one builder under the previous
/// classic reducer's observer identity, and that its state continues without a replay.
/// </summary>
public class when_verifying_reducer_migration_against_the_pinned_kernel : a_generated_application
{
    const string FoldPath = when_migrating_a_classic_reducer.FoldPath;
    const string ProbeFile = "ReducerMigrationMongoProbe.cs";

    static readonly string _source = when_migrating_a_classic_reducer.Source.Replace(
        "return new Total(context.Event.Id, context.Event.Amount);",
        "return new Total(context.Event.Id, (context.State?.Amount ?? 0m) + context.Event.Amount);",
        StringComparison.Ordinal);

    protected override bool DeferPlan => true;

    [MongoFact]
    async Task should_keep_one_builder_with_the_previous_observer_id_when_configured()
    {
        try
        {
            // Both phases share one event store namespace and one event source, so phase two sees phase one's observer state.
            var eventStoreNamespace = Guid.NewGuid().ToString("N");
            var key = Guid.NewGuid();
            var loaded = await when_rendering_a_pure_reducer.Load(_source);

            RenderApplication(when_rendering_a_pure_reducer.Plan(loaded));
            AddGeneratedSpecification(ProbeFile, ClassicProbe(eventStoreNamespace, key));
            await BuildAndProbe("classic", "ClassicProbe.should_reduce_with_the_classic_reducer");

            var evidence = ReducerMigrationEvidence.FromExistingFile(when_migrating_a_classic_reducer.ReadModel(loaded, "Total"), FoldPath, ReadGeneratedFile(FoldPath));
            Assert.Equal(when_migrating_a_classic_reducer.ObserverId, evidence.ObserverId);
            var profile = CratisRendering.WithReducerMigration(CratisRendering.CreateProfile("Projects", new("Projects", "Projects")), "Projects", [evidence], canRemoveFiles: true);
            var migration = when_selecting_self_reducing_rendering.Plan(loaded, profile);
            Assert.True(migration.Success, string.Join(Environment.NewLine, migration.Diagnostics));
            Assert.IsType<CratisPublicationCheck.Compatible>(CratisRendering.CheckPublication(migration, ReadExisting));
            PublishWithRemovals(migration);
            Assert.False(GeneratedFileExists(FoldPath));

            AddGeneratedSpecification(ProbeFile, MigratedProbe(eventStoreNamespace, key));
            await BuildAndProbe("migrated", "MigratedProbe.should_continue_with_one_builder_under_the_previous_observer_id");
        }
        finally
        {
            Cleanup();
        }
    }

    async Task BuildAndProbe(string phase, string probe)
    {
        var build = await Run($"reducer-migration-{phase}-build.log", "build", "Projects.csproj", "-c", "Debug", "-t:Rebuild", "-warnaserror", "-p:NoWarn=CS7022", "--nologo");
        Assert.Equal(string.Empty, BuildWarnings(build));
        var test = await Run($"reducer-migration-{phase}-probe.log", "test", "Projects.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", $"FullyQualifiedName~{probe}", "--nologo");
        Assert.Contains("Passed!", test, StringComparison.Ordinal);
    }

    string? ReadExisting(string relativePath) => GeneratedFileExists(relativePath) ? ReadGeneratedFile(relativePath) : null;

    static string ClassicProbe(string eventStoreNamespace, Guid key) => $$"""
        {{Usings}}

        public class ClassicProbe
        {
            [Fact]
            public async Task should_reduce_with_the_classic_reducer()
            {
        {{Connect(eventStoreNamespace, key)}}
                var appended = await store.EventLog.Append(sourceId, new OrderPlaced(key, 10m));
                Assert.True(appended.IsSuccess, $"Append failed: {appended}");
                await store.Reducers.WaitTillReachesEventSequenceNumber<Fold>(appended.SequenceNumber, TimeSpan.FromSeconds(60));
                Assert.Equal(10m, (await store.ReadModels.GetInstanceById<Total>(sourceId))!.Amount);
                Assert.Equal("{{when_migrating_a_classic_reducer.ObserverId}}", Assert.Single(store.Reducers.GetAllHandlers()).Id.Value);
                Assert.Empty(await store.Reducers.GetFailedPartitionsFor<Fold>());
            }
        }
        """;

    static string MigratedProbe(string eventStoreNamespace, Guid key) => $$"""
        {{Usings}}

        public class MigratedProbe
        {
            [Fact]
            public async Task should_continue_with_one_builder_under_the_previous_observer_id()
            {
        {{Connect(eventStoreNamespace, key)}}
                // A second builder for Total would surface as AmbiguousReadModelBuilder and fail registration above.
                var handler = Assert.Single(store.Reducers.GetAllHandlers());
                Assert.Equal("{{when_migrating_a_classic_reducer.ObserverId}}", handler.Id.Value);
                Assert.Equal(handler.Id, store.Reducers.GetHandlerForReadModelType(typeof(Total)).Id);

                // The observer resumes from phase one: one new fact accumulates onto the classic amount, with no replay.
                var appended = await store.EventLog.Append(sourceId, new OrderPlaced(key, 5m));
                Assert.True(appended.IsSuccess, $"Append failed: {appended}");
                await handler.WaitTillReachesEventSequenceNumber(appended.SequenceNumber, TimeSpan.FromSeconds(60));
                Assert.Equal(15m, (await store.ReadModels.GetInstanceById<Total>(sourceId))!.Amount);
                Assert.Empty(await handler.GetFailedPartitions());
            }
        }
        """;

    static string Connect(string eventStoreNamespace, Guid key) => $$"""
                var address = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
                Assert.False(string.IsNullOrWhiteSpace(address));
                using var client = new ChronicleClient(ChronicleOptions.FromConnectionString(new Cratis.Chronicle.Connections.ChronicleConnectionString(address!)),
                    namingPolicy: new CamelCaseNamingPolicy());
                var store = await client.GetEventStore("Stage464ReducerMigration", "{{eventStoreNamespace}}");
                var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(90));
                Assert.True(registration.IsSuccess, $"Registration failed: {registration.Failure}; {string.Join("; ", registration.Failures)}");
                var key = Guid.Parse("{{key}}");
                EventSourceId sourceId = key;
        """;

    static string Usings => """
        using Cratis.Chronicle;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Reducers;
        using Cratis.Chronicle.Registrations;
        using Cratis.Serialization;
        using Projects.Orders.Ordering.PlaceOrder;
        using Projects.Orders.Ordering.Totals;
        using Xunit;

        namespace Projects.ReducerMigrationMongoProbe;
        """;
}
#endif
