// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Verifies generated host wiring and routed command appends through the hosted Arc pipeline.
/// </summary>
public class when_verifying_routed_appends_against_mongo : a_generated_application
{
    const string Source = """
        concept AccountId : Uuid
        concept RouteId : Uuid
        concept Period : String
        concept Month : Int
        eventsource Account
          identifier AccountId
          stream UuidEntries
            streamId RouteId
          stream TextEntries
            streamId Period
          stream CompositeEntries
            streamId
              period Period
              month Month
          stream Notes
        module Banking
          feature Entries
            slice StateChange RecordUuid
              command RecordUuid
                accountId AccountId identifier
                routeId RouteId
                stream Account.UuidEntries
                  streamId = routeId
                produces event UuidRecorded
                  for accountId
                  routeId RouteId = routeId
            slice StateChange RecordText
              command RecordText
                accountId AccountId identifier
                period Period
                stream Account.TextEntries
                  streamId = period
                produces event TextRecorded
                  for accountId
                  period Period = period
            slice StateChange RecordComposite
              command RecordComposite
                accountId AccountId identifier
                period Period
                month Month
                stream Account.CompositeEntries
                  streamId
                    period = period
                    month = month
                produces event CompositeRecorded
                  for accountId
                  period Period = period
                  month Month = month
            slice StateChange RecordNote
              command RecordNote
                accountId AccountId identifier
                stream Account.Notes
                produces event NoteRecorded
                  for accountId
        """;

    const string Probe = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        using System.Reflection;
        using Cratis.Arc.MongoDB;
        using Cratis.Chronicle.EventSources;
        using Cratis.Chronicle.Registrations;
        using Routes.Common;
        using Xunit;

        public partial class Program
        {
            internal static readonly TaskCompletionSource<WebApplication> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal static WebApplication? ProbeApplication;

            static partial void ConfigureServices(WebApplicationBuilder builder)
            {
                builder.Configuration["Cratis:Chronicle:ConnectionString"] = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION") ?? "chronicle://localhost:35100";
                builder.Configuration["Cratis:Chronicle:EventStore"] = "Stage177Routes" + Guid.NewGuid().ToString("N");
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
                // This is copied from the emitted Program.cs, not a separately maintained hosting recipe.
                __GENERATED_BOOTSTRAP__
                return builder.Build();
            }
        }

        namespace Routes.HostProbe
        {
        public class RouteProbe
        {
            [Fact]
            public async Task should_wire_the_generated_host()
            {
                await using var app = Program.BuildProbe();
                var sources = app.Services.GetRequiredService<IEventSources>();
                await sources.Discover();
                var definition = sources.GetFor(typeof(Routes.EventSources.AccountEventSource));
                Assert.Equal("Account", definition.Name);
                Assert.Equal(4, definition.Streams.Count);
                using var scope = app.Services.CreateScope();
                Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICommandPipeline>());
            }

            [Fact]
            public async Task should_append_each_route_through_the_generated_host()
            {
                // Run the actual generated entry point, including AddCratis and UseCratis.
                var entry = typeof(Program).GetMethod("<Main>$", BindingFlags.Static | BindingFlags.NonPublic)!;
                var running = (Task)entry.Invoke(null, [Array.Empty<string>()])!;
                WebApplication? app = null;
                try
                {
                    var startup = await Task.WhenAny(Program.Started.Task, running).WaitAsync(TimeSpan.FromSeconds(120));
                    if (startup == running) await running;
                    app = await Program.Started.Task.WaitAsync(TimeSpan.FromSeconds(120));
                    using var scope = app.Services.CreateScope();
                    var services = scope.ServiceProvider;
                    var store = services.GetRequiredService<IEventStore>();
                    var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(90));
                    Assert.True(registration.IsSuccess, $"Registration failed: {registration.Failure}; {string.Join("; ", registration.Failures)}");
                    var pipeline = services.GetRequiredService<ICommandPipeline>();
                    var account = new AccountId(Guid.NewGuid());
                    var route = new RouteId(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
                    await Execute(new Routes.Banking.Entries.RecordUuid.RecordUuid(account, route));
                    await Execute(new Routes.Banking.Entries.RecordText.RecordText(account, new Period("Caf\u00e9")));
                    await Execute(new Routes.Banking.Entries.RecordComposite.RecordComposite(account, new Period("a|b%"), new Month(-7)));
                    await Execute(new Routes.Banking.Entries.RecordNote.RecordNote(account));
                    var invalid = await pipeline.Execute(new Routes.Banking.Entries.RecordText.RecordText(account, new Period("Cafe\u0301")), services);
                    Assert.False(invalid.IsValid);
                    Assert.True(invalid.IsAuthorized);
                    Assert.False(invalid.HasExceptions, string.Join("; ", invalid.ExceptionMessages));
                    Assert.Equal("Period", Assert.Single(Assert.Single(invalid.ValidationResults).Members));
                    var appended = await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, (EventSourceId)account);
                    Assert.Equal(4, appended.Count);
                    Verify<Routes.Banking.Entries.RecordUuid.UuidRecorded>("UuidEntries", "3fa85f64-5717-4562-b3fc-2c963f66afa6");
                    Verify<Routes.Banking.Entries.RecordText.TextRecorded>("TextEntries", "Caf\u00e9");
                    Verify<Routes.Banking.Entries.RecordComposite.CompositeRecorded>("CompositeEntries", "a%7Cb%25|-7");
                    Verify<Routes.Banking.Entries.RecordNote.NoteRecorded>("Notes", EventStreamId.Default);

                    async Task Execute(object command)
                    {
                        var result = await pipeline.Execute(command, services);
                        Assert.True(result.IsSuccess, $"Command failed: {string.Join("; ", result.ExceptionMessages)}; {string.Join("; ", result.ValidationResults)}");
                    }

                    void Verify<T>(string stream, string id)
                    {
                        var fact = Assert.Single(appended, fact => fact.Content is T);
                        Assert.Equal((EventSourceId)account, fact.Context.EventSourceId);
                        Assert.Equal("Account", fact.Context.EventSourceType.Value);
                        Assert.Equal(stream, fact.Context.EventStreamType.Value);
                        Assert.Equal(id, fact.Context.EventStreamId.Value);
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
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Routes"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("routes"), "routes", "Routes.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Routes", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));

        return CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Routes", "Routes"));
    }

    [Fact]
    async Task should_wire_the_generated_host_without_a_kernel() => await Verify("should_wire_the_generated_host");

#pragma warning disable CRSPEC0004 // MongoFact derives from FactAttribute and is discovered by xUnit.
    [MongoFact]
    async Task should_append_all_routes_when_configured() => await Verify("should_append_each_route_through_the_generated_host");
#pragma warning restore CRSPEC0004

    async Task Verify(string fact)
    {
        try
        {
            var program = ReadGeneratedFile("Program.cs");
            var start = program.IndexOf("var builder =", StringComparison.Ordinal);
            var end = program.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
            Assert.True(start >= 0 && end > start);
            var bootstrap = program[start..end].Replace("CreateBuilder(args)", "CreateBuilder(Array.Empty<string>())", StringComparison.Ordinal);
            AddGeneratedSpecification("RouteProbe.cs", Probe.Replace("__GENERATED_BOOTSTRAP__", bootstrap, StringComparison.Ordinal));
            var build = await Run("routes-build.log", "build", "Routes.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var output = await Run("routes-probe.log", "test", "Routes.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", $"FullyQualifiedName~RouteProbe.{fact}", "--nologo");
            Assert.Contains("Passed!", output, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
