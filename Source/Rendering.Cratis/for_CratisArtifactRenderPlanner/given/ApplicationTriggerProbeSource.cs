// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class ApplicationTriggerProbeSource
{
    internal const string Host = """
        using Cratis.Chronicle;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.Hosting;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using Microsoft.Extensions.Logging;
        namespace Reactions;
        public static class Stage343Host
        {
            public static WebApplication Create(string connection, string storeName)
            {
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>(), ContentRootPath = Path.GetTempPath(), EnvironmentName = "Production" });
                builder.Logging.ClearProviders();
                builder.Logging.AddSimpleConsole();
                builder.Logging.SetMinimumLevel(LogLevel.Debug);
                builder.WebHost.UseUrls("http://127.0.0.1:0");
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cratis:Chronicle:EventStore"] = storeName,
                    ["Cratis:Chronicle:ConnectionString"] = connection
                });
                builder.AddCratis(configureChronicleBuilder: chronicle => chronicle.WithCamelCaseNamingPolicy());
                var app = builder.Build();
                app.UseCratis();
                return app;
            }
        }
        """;

    internal const string Probe = """
        using Cratis.Chronicle;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Occurrences;
        using Microsoft.Extensions.DependencyInjection;
        using Reactions.Work.Processing.Batches;
        using Xunit;
        namespace Reactions.KernelProbe;
        public class ApplicationTriggerProbe
        {
            const string EffectName = "__EFFECT_NAME__";
            [Fact]
            public async Task should_append_once_per_namespace_and_skip_a_receipted_redelivery()
            {
                var connection = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
                Assert.False(string.IsNullOrWhiteSpace(connection));
                await using var app = Stage343Host.Create(connection!, $"stage343-{Guid.NewGuid():N}");
                try
                {
                    await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(90));
                    var client = app.Services.GetRequiredService<IChronicleClient>();
                    var injectedStore = app.Services.GetRequiredService<IEventStore>();
                    var firstStore = await client.GetEventStore(injectedStore.Name, "recording-one");
                    var secondStore = await client.GetEventStore(injectedStore.Name, "recording-two");
                    await firstStore.DiscoverAll().WaitAsync(TimeSpan.FromSeconds(90));
                    await secondStore.DiscoverAll().WaitAsync(TimeSpan.FromSeconds(90));
                    await Verify(firstStore, injectedStore, client);
                    Assert.Empty(await secondStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                    await Verify(secondStore, injectedStore, client);
                    Assert.Single(await firstStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                    Assert.Empty(await injectedStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First));
                }
                finally
                {
                    await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(30));
                }
            }

            static async Task Verify(IEventStore store, IEventStore injectedStore, IChronicleClient client)
            {
                var payload = new BatchRequested("destination-one");
                var first = await store.Occurrences.Record(payload, "same-ingress");
                Assert.Equal(OccurrenceRecordStatus.Recorded, first.Status);
                var duplicate = await store.Occurrences.Record(payload, "same-ingress");
                Assert.Equal(OccurrenceRecordStatus.AlreadyRecorded, duplicate.Status);
                Assert.Equal(first.Id, duplicate.Id);
                var key = new EffectKey(first.Id, new(EffectName), new(first.Id, 0));
                var receipt = await WaitForReceipt(store, key);
                var appended = Assert.IsType<Appended>(receipt.Outcome);
                Assert.Equal(appended.First, appended.Last);
                var before = await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
                var fact = Assert.Single(before);
                Assert.Equal("destination-one", fact.Context.EventSourceId.Value);
                Assert.Equal("destination-one", Assert.IsType<BatchStarted>(fact.Content).BatchId);
                Assert.Equal(appended.First, fact.Context.SequenceNumber);
                // Deliver the same durable occurrence to the generated handler again, with a later lease.
                // This exercises the durable receipt fast path, not a kernel retry scheduling policy.
                var redelivery = new OccurrenceContext(first.Id, new("__TRIGGER_ID__"), OccurrenceKind.Application, null,
                    receipt.RecordedAt, default!, 2, new(first.Id, receipt.WrittenBy.Epoch + 1)) { Namespace = store.Namespace };
                await new ProcessBatch(client, injectedStore).Handle(payload, redelivery, CancellationToken.None);
                var after = await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
                Assert.Single(after);
                Assert.Equal(fact.Context.SequenceNumber, after.Single().Context.SequenceNumber);
                var retained = await store.Occurrences.GetEffectReceipt(key);
                Assert.NotNull(retained);
                Assert.Equal(receipt.RecordedAt, retained.RecordedAt);
                Assert.Equal(receipt.WrittenBy, retained.WrittenBy);
            }

            static async Task<EffectReceipt> WaitForReceipt(IEventStore store, EffectKey key)
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
                while (!deadline.IsCancellationRequested)
                {
                    var receipt = await store.Occurrences.GetEffectReceipt(key).WaitAsync(deadline.Token);
                    if (receipt is not null) return receipt;
                    await Task.Delay(250, deadline.Token);
                }
                throw new TimeoutException("No durable occurrence production receipt was written.");
            }
        }
        """;
}
#endif
