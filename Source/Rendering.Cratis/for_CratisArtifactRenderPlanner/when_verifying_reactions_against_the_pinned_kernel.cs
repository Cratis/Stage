// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticAutomationArtifactRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_verifying_reactions_against_the_pinned_kernel : a_generated_application
{
    const string Probe = """
        using System.Reflection;
        using Cratis.Chronicle.Observation;
        using Cratis.Chronicle.Registrations;
        using Reactions.Work.Processing.FollowUp;
        using Reactions.Work.Processing.Start;
        using Xunit;

        public partial class Program
        {
            internal static readonly TaskCompletionSource<WebApplication> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal static WebApplication? ProbeApplication;

            static partial void ConfigureServices(WebApplicationBuilder builder)
            {
                builder.Configuration["Cratis:Chronicle:ConnectionString"] = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
                builder.Configuration["Cratis:Chronicle:EventStore"] = "Stage207Reactions" + Guid.NewGuid().ToString("N");
            }

            static partial void ConfigureApplication(WebApplication app)
            {
                ProbeApplication = app;
                app.Urls.Clear();
                app.Urls.Add("http://127.0.0.1:0");
                app.Lifetime.ApplicationStarted.Register(() => Started.TrySetResult(app));
            }

            internal static WebApplication BuildProbe()
            {
                __GENERATED_BOOTSTRAP__
                return builder.Build();
            }
        }

        namespace Reactions.KernelProbe
        {
        public class ReactionProbe
        {
            [Fact]
            public async Task should_settle_event_reactions()
            {
                var entry = typeof(Program).GetMethod("<Main>$", BindingFlags.Static | BindingFlags.NonPublic)!;
                var running = (Task)entry.Invoke(null, [Array.Empty<string>()])!;
                WebApplication? app = null;
                try
                {
                    var startup = await Task.WhenAny(Program.Started.Task, running).WaitAsync(TimeSpan.FromSeconds(120));
                    if (startup == running) await running;
                    app = await Program.Started.Task.WaitAsync(TimeSpan.FromSeconds(120));
                    using var scope = app.Services.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
                    var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(90));
                    Assert.True(registration.IsSuccess, $"Registration failed: {registration.Failure}; {string.Join("; ", registration.Failures)}");
                    Assert.StartsWith("sem1:", store.Reactors.GetHandlerFor<Acknowledge>().Id.Value, StringComparison.Ordinal);
                    await Verify("positive", 5m, true);
                    await Verify("zero", 0m, false);
                    Assert.Empty(await store.Reactors.GetFailedPartitionsFor<Acknowledge>());
                    Assert.Empty(await store.Reactors.GetFailedPartitionsFor<CompletePositive>());
                    await VerifyUnmarked();

                    async Task VerifyUnmarked()
                    {
                        var append = await store.EventLog.Append("unmarked-trigger", new UntrustedStarted("unmarked-result", 5m));
                        Assert.True(append.IsSuccess);
                        var completion = await append.WaitForCompletion(TimeSpan.FromSeconds(90));
                        Assert.False(completion.TimedOut);
                        Assert.False(completion.IsSuccess);
                        Assert.NotEmpty(completion.FailedPartitions);
                        Assert.NotEmpty(await store.Reactors.GetFailedPartitionsFor<UnmarkedWorker>());
                        Assert.Empty(await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, "unmarked-result"));
                    }

                    async Task Verify(string id, decimal amount, bool completes)
                    {
                        var occurred = new DateTimeOffset(2026, 10, 12, 12, 30, 0, TimeSpan.Zero);
                        // Independent reactors must not race a command's optimistic-concurrency guard.
                        // The payload identifies the command's source, distinct from the trigger's source.
                        var eventSource = "trigger-" + id;
                        var append = await store.EventLog.Append(eventSource, new Started(id, amount), occurred: occurred);
                        Assert.True(append.IsSuccess);
                        var completion = await append.WaitForCompletion(TimeSpan.FromSeconds(90));
                        Assert.True(completion.IsSuccess, $"Observer completion failed: {completion.TimedOut}; {string.Join("; ", completion.OutstandingObservers)}; {System.Text.Json.JsonSerializer.Serialize(completion.FailedPartitions)}");
                        var events = await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, eventSource);
                        var acknowledged = Assert.Single(events, fact => fact.Content is Acknowledged);
                        Assert.Equal(occurred, ((Acknowledged)acknowledged.Content).ScheduledAt);
                        Assert.Equal(occurred, acknowledged.Context.Occurred);
                        Assert.Equal((EventSourceId)eventSource, acknowledged.Context.EventSourceId);
                        Assert.Equal(EventSourceType.Default, acknowledged.Context.EventSourceType);
                        Assert.Equal(EventStreamType.All, acknowledged.Context.EventStreamType);
                        Assert.Equal(EventStreamId.Default, acknowledged.Context.EventStreamId);
                        var completed = await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, id);
                        Assert.Equal(completes ? 1 : 0, completed.Count);
                        if (completes)
                        {
                            var fact = Assert.Single(completed);
                            Assert.Equal(amount, Assert.IsType<Completed>(fact.Content).Amount);
                            Assert.Equal((EventSourceId)id, fact.Context.EventSourceId);
                        }
                        Assert.Equal(2, events.Count);
                    }
                }
                finally
                {
                    app ??= Program.ProbeApplication;
                    if (app is not null)
                    {
                        await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(30));
                        await running.WaitAsync(TimeSpan.FromSeconds(30));
                        await app.DisposeAsync();
                    }
                }
            }
        }
        }
        """;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = "policy CanComplete\n  require role \"Automation\"\n" + an_automation.Source
            .Replace("      command CompleteWork", "      command CompleteWork\n        authorize CanComplete", StringComparison.Ordinal)
            .Replace("      reaction CompletePositive", "      reaction CompletePositive\n        runs as system role \"Automation\"", StringComparison.Ordinal);
        source += """

                  event UntrustedStarted
                    id String
                    amount Decimal
                  reaction UnmarkedWorker
                    when UntrustedStarted
                      invokes CompleteWork
                        id = id
                        amount = amount
            """;
        var model = an_automation.Compile(source);

        return CratisRendering.Plan(model, SemanticExecutionPlan.Compile(model).Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Reactions", "Reactions"));
    }

#pragma warning disable CRSPEC0004 // MongoFact derives from FactAttribute and is discovered by xUnit.
    [MongoFact]
    async Task should_settle_event_reactions_when_configured()
#pragma warning restore CRSPEC0004
    {
        try
        {
            var program = ReadGeneratedFile("Program.cs");
            var start = program.IndexOf("var builder =", StringComparison.Ordinal);
            var end = program.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
            Assert.True(start >= 0 && end > start);
            var bootstrap = program[start..end].Replace("CreateBuilder(args)", "CreateBuilder(Array.Empty<string>())", StringComparison.Ordinal);
            AddGeneratedSpecification("ReactionProbe.cs", Probe.Replace("__GENERATED_BOOTSTRAP__", bootstrap, StringComparison.Ordinal));
            var build = await Run("reactions-build.log", "build", "Reactions.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var output = await Run("reactions-probe.log", "test", "Reactions.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", "FullyQualifiedName~ReactionProbe.should_settle_event_reactions", "--nologo");
            Assert.Contains("Passed!", output, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
