// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class MixedReactionRefusalProbeSource
{
    internal const string Model = """
        module Work
          feature Processing
            slice Automation FollowUp
              event Started
                id String
                audit String
                amount Decimal
              event QuietStarted
                id String
                audit String
                amount Decimal
              event PrefixOne
                marker String
              event PrefixTwo
                marker String
              event Completed
                marker String
              event SentinelReached
                marker String
              command Begin
                id String identifier
                audit String
                amount Decimal
                produces Started
                  for id
                  id = id
                  audit = audit
                  amount = amount
              command QuietBegin
                id String identifier
                audit String
                amount Decimal
                produces QuietStarted
                  for id
                  id = id
                  audit = audit
                  amount = amount
              command Complete
                id String identifier
                amount Decimal
                validate
                  amount > 0 message "Positive amount required"
                produces Completed
                  for id
                  marker = id
              command Sentinel
                id String identifier
                produces SentinelReached
                  for id
                  marker = id
              reaction Worker
                runs as system
                when Started
                  produces PrefixOne
                    for id
                    marker = id
                  produces PrefixTwo
                    for audit
                    marker = id
                  invokes Complete
                    id = id
                    amount = amount
                  invokes Sentinel
                    id = id
              reaction Quiet
                runs as system
                when QuietStarted
                  produces PrefixOne
                    for id
                    marker = id
                  produces PrefixTwo
                    for audit
                    marker = id
                  invokes Complete
                    id = id
                    amount = amount
                    on refused
                      acknowledge
                  invokes Sentinel
                    id = id
              specification Unhandled
                when append Started
                  for "unhandled"
                  id = "unhandled"
                  audit = "audit-unhandled"
                  amount = 0
                then error "Positive amount required"
              specification Acknowledged
                when append QuietStarted
                  for "ack"
                  id = "ack"
                  audit = "audit-ack"
                  amount = 0
                then PrefixOne
                  for "ack"
                  marker = "ack"
                then PrefixTwo
                  for "audit-ack"
                  marker = "ack"
              specification ValidControl
                when append QuietStarted
                  for "control"
                  id = "control"
                  audit = "audit-control"
                  amount = 1
                then PrefixOne
                  for "control"
                  marker = "control"
                then PrefixTwo
                  for "audit-control"
                  marker = "control"
                then Completed
                  for "control"
                  marker = "control"
                then SentinelReached
                  for "control"
                  marker = "control"
        """;

    internal const string Host = """
        using Microsoft.Extensions.Configuration;

        public partial class Program
        {
            public static readonly TaskCompletionSource<WebApplication> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public static WebApplication? ProbeApplication;

            static partial void ConfigureServices(WebApplicationBuilder builder)
            {
                var endpoint = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
                ParseKernelEndpoint(endpoint);
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cratis:Chronicle:ConnectionString"] = endpoint,
                    ["Cratis:Chronicle:EventStore"] = $"stage342{Guid.NewGuid():N}"
                });
            }

            static Uri ParseKernelEndpoint(string? endpoint)
            {
                if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                    throw new Exception("The Stage 342 probe requires the CI-owned loopback kernel on port 35000.");
                RequireKernelEndpoint(uri);
                return uri;
            }

            static void RequireKernelEndpoint(Uri uri)
            {
                if (uri.Scheme != "chronicle")
                    throw new Exception("The Stage 342 probe requires the CI-owned loopback kernel on port 35000.");
                if (!uri.IsLoopback)
                    throw new Exception("The Stage 342 probe requires the CI-owned loopback kernel on port 35000.");
                if (uri.Port != 35000)
                    throw new Exception("The Stage 342 probe requires the CI-owned loopback kernel on port 35000.");
            }

            static partial void ConfigureApplication(WebApplication app)
            {
                ProbeApplication = app;
                app.Urls.Clear();
                app.Urls.Add("http://127.0.0.1:0");
                app.Lifetime.ApplicationStarted.Register(() => Started.TrySetResult(app));
            }
        }
        """;

    internal const string Probe = """
        using System.Reflection;
        using System.Text.Json;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.Observation;
        using Cratis.Chronicle.Reactors;
        using Cratis.Chronicle.Registrations;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.Extensions.DependencyInjection;
        using Reactions.Work.Processing.FollowUp;
        using Xunit;

        namespace Reactions.KernelProbe;

        public class MixedRefusalProbe
        {
            [Fact]
            public async Task should_prove_retention_acknowledgement_and_durable_redelivery()
            {
                var entry = typeof(Program).GetMethod("<Main>$", BindingFlags.Static | BindingFlags.NonPublic)!;
                var running = (Task)entry.Invoke(null, [Array.Empty<string>()])!;
                WebApplication? app = null;
                try
                {
                    var startup = await Task.WhenAny(Program.Started.Task, running).WaitAsync(TimeSpan.FromSeconds(90));
                    if (startup == running) await running;
                    app = await Program.Started.Task.WaitAsync(TimeSpan.FromSeconds(90));
                    using var scope = app.Services.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
                    var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(60));
                    Assert.True(registration.IsSuccess, $"Registration: {registration.Failure}; {string.Join(';', registration.Failures)}");
                    var input = await store.EventLog.Append("unhandled", new Started("unhandled", "audit-unhandled", 0m));
                    Assert.True(input.IsSuccess, "Original input append failed.");
                    var sequence = input.SequenceNumber;
                    var initial = await Failure(store, sequence, null, 0);
                    await AssertWorld(store, "Unhandled");
                    var prefix = await History(store);
                    var baseline = await Failure(store, sequence, initial.Id, 0);
                    var outcome = await store.Reactors.RetryFailedPartitionFor<Worker>("unhandled");
                    Assert.Equal(ReactorPartitionRetryOutcome.Started, outcome);
                    var retried = await Failure(store, sequence, baseline.Id, baseline.Attempts.Count());
                    Assert.True(retried.Attempts.Count() > baseline.Attempts.Count());
                    var redelivered = await History(store);
                    Assert.Equal(prefix.Select(Identity), redelivered.Select(Identity));
                    await AssertWorld(store, "Unhandled");
                    await QuietInput(store, "ack", 0m, "Acknowledged");
                    await QuietInput(store, "control", 1m, "ValidControl");
                    Assert.Empty(await store.Reactors.GetFailedPartitionsFor<Quiet>());
                    Console.WriteLine("STAGE342: retained prefix; acknowledged stop; real failed-partition recovery; durable dedup verified.");
                }
                finally
                {
                    app ??= Program.ProbeApplication;
                    if (app is not null)
                    {
                        try
                        {
                            await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(10));
                            await running.WaitAsync(TimeSpan.FromSeconds(10));
                        }
                        finally
                        {
                            await app.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
                        }
                    }
                }
            }

            static async Task QuietInput(IEventStore store, string id, decimal amount, string expectation)
            {
                var appended = await store.EventLog.Append(id, new QuietStarted(id, $"audit-{id}", amount));
                Assert.True(appended.IsSuccess, $"Input {id} failed.");
                var completed = await appended.WaitForCompletion(TimeSpan.FromSeconds(30));
                Assert.True(completed.IsSuccess, $"Quiet/{id}: timedOut={completed.TimedOut}; failed={JsonSerializer.Serialize(completed.FailedPartitions)}; outstanding={string.Join(';', completed.OutstandingObservers)}");
                Assert.False(completed.TimedOut);
                Assert.Empty(completed.FailedPartitions);
                Assert.Empty(completed.OutstandingObservers);
                await AssertWorld(store, expectation);
            }

            static async Task<FailedPartition> Failure(IEventStore store, EventSequenceNumber sequence, FailedPartitionId? id, int after)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var ticks = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
                string last = "No failed partition observed.";
                try
                {
                    do
                    {
                        var snapshot = await ReadWorkerFailure(store, timeout.Token);
                        last = snapshot.Diagnostic;
                        if (!IsNewerFailure(snapshot.Failure, id, after)) continue;
                        AssertNewAttempts(snapshot.Failure!, sequence, after);
                        return snapshot.Failure!;
                    } while (await ticks.WaitForNextTickAsync(timeout.Token));
                }
                catch (OperationCanceledException)
                {
                    throw new Exception($"No newer persisted refusal for sequence {sequence}, after {after} attempts. Last snapshot: {last[..Math.Min(last.Length, 16000)]}");
                }
                throw new Exception("The failure-observation timer stopped unexpectedly.");
            }

            static async Task<(string Diagnostic, FailedPartition? Failure)> ReadWorkerFailure(IEventStore store, CancellationToken token)
            {
                var failures = (await store.Reactors.GetFailedPartitionsFor<Worker>().WaitAsync(token)).ToArray();
                var diagnostic = JsonSerializer.Serialize(failures);
                Assert.All(failures, failure => Assert.Equal("unhandled", failure.Partition.Value));
                return (diagnostic[..Math.Min(diagnostic.Length, 16000)], failures.SingleOrDefault());
            }

            static bool MatchesFailureId(FailedPartition failure, FailedPartitionId? expectedId)
            {
                if (expectedId is null) return true;
                return failure.Id == expectedId;
            }

            static bool IsNewerFailure(FailedPartition? failure, FailedPartitionId? expectedId, int after)
            {
                if (failure is null) return false;
                if (!MatchesFailureId(failure, expectedId)) return false;
                return failure.Attempts.Count() > after;
            }

            static void AssertNewAttempts(FailedPartition failure, EventSequenceNumber sequence, int after)
            {
                Assert.False(failure.IsQuarantined);
                var attempts = failure.Attempts.Skip(after).ToArray();
                Assert.All(attempts, attempt => Assert.Equal(sequence, attempt.SequenceNumber));
                Assert.All(attempts, attempt => Assert.Contains(attempt.Messages, message => message.Contains("Positive amount required", StringComparison.Ordinal)));
            }

            static async Task<Cratis.Chronicle.Events.AppendedEvent[]> History(IEventStore store)
            {
                var events = await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First).WaitAsync(TimeSpan.FromSeconds(30));
                Assert.InRange(events.Count, 1, 16);
                return events.ToArray();
            }

            static string Identity(Cratis.Chronicle.Events.AppendedEvent item) => $"{item.Context.SequenceNumber}:{Shape(item)}";

            static string Shape(Cratis.Chronicle.Events.AppendedEvent item)
            {
                var marker = item.Content switch
                {
                    PrefixOne value => value.Marker,
                    PrefixTwo value => value.Marker,
                    Completed value => value.Marker,
                    SentinelReached value => value.Marker,
                    _ => string.Empty
                };
                return $"{item.Content.GetType().Name}|{item.Context.EventSourceId.Value}|{marker}";
            }

            static async Task AssertWorld(IEventStore store, string name)
            {
                var expected = JsonSerializer.Deserialize<Dictionary<string, string[]>>(__REFERENCE_LITERAL__)!;
                var names = name == "Unhandled" ? new[] { "Unhandled" } : name == "Acknowledged" ? new[] { "Unhandled", "Acknowledged" } : new[] { "Unhandled", "Acknowledged", "ValidControl" };
                Assert.Equal(names.SelectMany(key => expected[key]), (await History(store)).Select(Shape));
            }
        }
        """;
}
#endif
