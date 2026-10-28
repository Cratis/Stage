// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_verifying_reaction_bodies_against_the_pinned_kernel : a_generated_application
{
    const string HostSupport = """
        public partial class Program
        {
            static partial void ConfigureServices(WebApplicationBuilder builder)
            {
                builder.Configuration["Cratis:Chronicle:ConnectionString"] = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
                builder.Configuration["Cratis:Chronicle:EventStore"] = "Stage829Bodies" + Guid.NewGuid().ToString("N");
            }

            static partial void ConfigureApplication(WebApplication app)
            {
                app.Urls.Clear();
                app.Urls.Add("http://127.0.0.1:0");
            }

            public static WebApplication BuildProbe()
            {
                __GENERATED_BOOTSTRAP__
                var app = builder.Build();
                ConfigureApplication(app);
                return app;
            }
        }
        """;

    const string Probe = """
        using Cratis.Chronicle.Observation;
        using Cratis.Chronicle.Reactors;
        using Cratis.Chronicle.Registrations;
        using Microsoft.Extensions.DependencyInjection;
        using __ACCEPTED_NAMESPACE__;
        using __REACTION_NAMESPACE__;
        using Xunit;
        using Xunit.Abstractions;

        namespace Reviews.KernelProbe;

        public class ReactionBodyProbe(ITestOutputHelper output)
        {
            [Fact]
            async Task should_settle_native_reaction_bodies()
            {
                using var overall = new CancellationTokenSource(TimeSpan.FromMinutes(4));
                await using var app = Program.BuildProbe();
                try
                {
                    await app.StartAsync().WaitAsync(TimeSpan.FromSeconds(120));
                    using var scope = app.Services.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
                    var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(90)).WaitAsync(overall.Token);
                    Assert.True(registration.IsSuccess, $"Registration failed: {registration.Failure}; {string.Join("; ", registration.Failures)}");
                    await Verify("3fa85f64-5717-4562-b3fc-2c963f66afa6", 10, true);
                    await Verify("3fa85f64-5717-4562-b3fc-2c963f66afa7", 5, false);
                    const string unsupported = "3fa85f64-5717-4562-b3fc-2c963f66afa8";
                    await Verify(unsupported, 13, true);
                    await VerifyFailure(unsupported);
                    Assert.Empty(await store.Reactors.GetFailedPartitionsFor<HighlightTopTalk>().WaitAsync(overall.Token));
                    Assert.Empty(await store.Reactors.GetFailedPartitionsFor<RecordReview>().WaitAsync(overall.Token));
                    var all = await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First).WaitAsync(overall.Token);
                    Assert.Equal(8, all.Count);
                    Assert.All(all, fact => Assert.True(fact.Content is TalkAccepted or TalkHighlighted or TalkReviewed));

                    async Task Verify(string source, int score, bool highlights)
                    {
                        var append = await store.EventLog.Append(source, new TalkAccepted(score)).WaitAsync(overall.Token);
                        Assert.True(append.IsSuccess, $"Append TalkAccepted({score}) on {source} failed: {append}");
                        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        await store.Reactors.WaitTillReachesEventSequenceNumber<HighlightTopTalk>(append.SequenceNumber, TimeSpan.FromSeconds(60)).WaitAsync(deadline.Token);
                        await store.Reactors.WaitTillReachesEventSequenceNumber<RecordReview>(append.SequenceNumber, TimeSpan.FromSeconds(60)).WaitAsync(deadline.Token);
                        var facts = await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, source).WaitAsync(overall.Token);
                        Assert.Equal(highlights ? 3 : 2, facts.Count);
                        var reviewed = Assert.Single(facts, fact => fact.Content is TalkReviewed);
                        Assert.Equal(score, Assert.IsType<TalkReviewed>(reviewed.Content).Score);
                        var highlighted = facts.Where(fact => fact.Content is TalkHighlighted).ToArray();
                        if (highlights) Assert.Equal(score, Assert.IsType<TalkHighlighted>(Assert.Single(highlighted).Content).Score);
                        else Assert.Empty(highlighted);
                        Assert.All(facts, fact => Assert.Equal(source, fact.Context.EventSourceId.Value));
                    }

                    async Task VerifyFailure(string source)
                    {
                        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        FailedPartition[] failures;
                        do
                        {
                            failures = (await store.Reactors.GetFailedPartitionsFor<ReturnUnsupportedValue>().WaitAsync(deadline.Token)).ToArray();
                            if (failures.Length > 0) break;
                            await Task.Delay(250, deadline.Token);
                        }
                        while (true);
                        var failure = Assert.Single(failures);
                        Assert.Equal(source, failure.Partition.Value);
                        Assert.NotEmpty(failure.Attempts);
                        var messages = failure.Attempts.SelectMany(attempt => attempt.Messages).ToArray();
                        Assert.NotEmpty(messages);
                        output.WriteLine($"Failed partition {source}: {string.Join("; ", messages)}");
                    }
                }
                finally
                {
                    await app.StopAsync().WaitAsync(TimeSpan.FromSeconds(30));
                }
            }
        }
        """;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var folder = Path.Combine(Path.GetTempPath(), "stage-829-reaction-bodies", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "Reactions"));
        try
        {
            File.WriteAllText(Path.Combine(folder, "Reaction.play"), Model);
            File.WriteAllText(Path.Combine(folder, "Reactions", "RecordReview.cs"), "return [new TalkReviewed(@event.Score)];");
            var loaded = SemanticModelLoader.LoadFromPathAsync(folder, null, "Reviews").GetAwaiter().GetResult();
            var profile = CratisRendering.CreateProfile("Reviews", new("Reviews", "Reviews") { Strict = true });
            var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
            {
                SemanticIdentities = loaded.SemanticIdentities,
                IdentityCatalog = loaded.IdentityCatalog,
                ImplementationRequirements = loaded.ImplementationRequirements,
                ImplementationContents = loaded.ImplementationContents,
                TypedContextDescriptors = loaded.TypedContextDescriptors,
                AttachmentDiagnostics = loaded.AttachmentDiagnostics
            };
            return new CratisArtifactRenderPlanner().Plan(request);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    const string Model = """
        domain Conference.Common
        concept TalkId : Uuid

        module CallForPapers
          feature Review
            slice StateChange Accepting
              command AcceptTalk
                talkId TalkId identifier
                score Int
                produces TalkAccepted
                  for talkId
                  score = score
              event TalkAccepted
                score Int

            slice Automation Highlighting
              event TalkHighlighted
                score Int
              event TalkReviewed
                score Int
              reaction HighlightTopTalk
                when TalkAccepted
                  ```csharp
                  if (@event.Score >= 9) { return [new TalkHighlighted(@event.Score)]; }
                  return [];
                  ```
              reaction RecordReview
                when TalkAccepted
                  file Reactions/RecordReview.cs
              reaction ReturnUnsupportedValue
                when TalkAccepted
                  ```csharp
                  if (@event.Score == 13) { return ["not an event"]; }
                  return [];
                  ```
        """;

    [Fact]
    async Task should_compile_the_reaction_body_probe_without_a_connection()
    {
        InstallProbe();
        var build = await Run("reaction-bodies-compile.log", "build", "Reviews.csproj", "-c", "Debug", "-warnaserror", "--nologo");
        Assert.Equal(string.Empty, BuildWarnings(build));
    }

    void InstallProbe()
    {
        var program = ReadGeneratedFile("Program.cs");
        var start = program.IndexOf("var builder =", StringComparison.Ordinal);
        var end = program.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        var bootstrap = program[start..end].Replace("CreateBuilder(args)", "CreateBuilder(Array.Empty<string>())", StringComparison.Ordinal);
        AddGeneratedHostSupport("ReactionBodyHost.cs", HostSupport.Replace("__GENERATED_BOOTSTRAP__", bootstrap, StringComparison.Ordinal));
        AddGeneratedSpecification("ReactionBodyProbe.cs", Probe
            .Replace("__ACCEPTED_NAMESPACE__", NamespaceOf("Accepting.cs"), StringComparison.Ordinal)
            .Replace("__REACTION_NAMESPACE__", NamespaceOf("HighlightTopTalk.cs"), StringComparison.Ordinal));
    }

    // The pinned kernel's /health includes Storage.MongoDB's named mongodb health check.
    static async Task WaitForKernelAndMongo()
    {
        var connection = new Uri(Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION")!);
        var health = new UriBuilder("https", connection.Host, connection.IsDefaultPort ? 35000 : connection.Port, "/health").Uri;
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            // Only the local development kernel uses a self-signed certificate.
            ServerCertificateCustomValidationCallback = (request, _, _, errors) => errors == System.Net.Security.SslPolicyErrors.None || request.RequestUri!.IsLoopback
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var last = "No health response received.";
        while (!deadline.IsCancellationRequested)
        {
            try
            {
                using var response = await client.GetAsync(health, deadline.Token);
                if (response.IsSuccessStatusCode) return;
                last = $"Health returned HTTP {(int)response.StatusCode}.";
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
            {
                last = exception.GetType().Name;
            }
            try
            {
                // Infrastructure readiness backoff, not an observer-completion wait.
                await Task.Delay(TimeSpan.FromSeconds(2), deadline.Token);
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested)
            {
                break;
            }
        }
        Assert.Fail($"Pinned Chronicle kernel and MongoDB did not become healthy within 120 seconds. {last}");
    }

    string NamespaceOf(string file) => ReadGeneratedFile(Assert.Single(GeneratedFiles(file)))
        .Split('\n').Single(line => line.StartsWith("namespace ", StringComparison.Ordinal))["namespace ".Length..].TrimEnd(';', '\r');

#pragma warning disable CRSPEC0004 // MongoFact derives from FactAttribute and is discovered by xUnit.
    [MongoFact]
    async Task should_settle_native_reaction_bodies_when_configured()
#pragma warning restore CRSPEC0004
    {
        try
        {
            await WaitForKernelAndMongo();
            InstallProbe();
            var build = await Run("reaction-bodies-build.log", "build", "Reviews.csproj", "-c", "Debug", "-warnaserror", "--nologo");
            Assert.Equal(string.Empty, BuildWarnings(build));
            var output = await Run("reaction-bodies-probe.log", "test", "Reviews.csproj", "-c", "Debug", "--no-build", "--no-restore", "--filter", "FullyQualifiedName~ReactionBodyProbe.should_settle_native_reaction_bodies", "--logger", "trx;LogFileName=reaction-body-inner.trx", "--results-directory", _evidence.FullName, "--nologo");
            Assert.Contains("Passed!", output, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup();
        }
    }
}
#endif
