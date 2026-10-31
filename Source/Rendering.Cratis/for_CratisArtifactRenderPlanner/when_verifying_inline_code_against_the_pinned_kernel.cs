// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_verifying_inline_code_against_the_pinned_kernel : a_generated_application
{
    protected override bool DeferPlan => true;

    [Fact]
    public Task should_compile_demo_http_probe_without_a_connection() => Compile("demo", "Billing");

    [Fact]
    public Task should_compile_min_a3_http_probe_without_a_connection() => Compile("min-a3", "Billing");

    [MongoFact]
    public Task should_verify_demo_when_configured() => Execute("demo", "Billing");

    [MongoFact]
    public Task should_verify_min_a3_when_configured() => Execute("min-a3", "Billing");

    async Task Compile(string sample, string project)
    {
        await Install(sample, project);
        var output = await Run("inline-compile.log", "build", project + ".csproj", "-c", "Debug", "-warnaserror", "--nologo");
        Assert.Equal(string.Empty, BuildWarnings(output));
    }

    async Task Install(string sample, string project, [CallerFilePath] string source = "")
    {
        var root = Path.Combine(Path.GetDirectoryName(source), "inline-code-runtime", sample);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(root, null, project);
        var profile = CratisRendering.CreateProfile(project, new(project, project) { Strict = true });
        var request = new ArtifactRenderRequest(loaded.Model, loaded.Plan, profile, new(ArtifactRenderScopeKind.Application, loaded.Model.Application.Id))
        {
            SemanticIdentities = loaded.SemanticIdentities,
            IdentityCatalog = loaded.IdentityCatalog,
            ImplementationRequirements = loaded.ImplementationRequirements,
            ImplementationContents = loaded.ImplementationContents,
            TypedContextDescriptors = loaded.TypedContextDescriptors,
            AttachmentDiagnostics = loaded.AttachmentDiagnostics
        };
        RenderApplication(new CratisArtifactRenderPlanner().Plan(request));
        AddGeneratedHostSupport("InlineHost.cs", HostSupport.Replace("__STORE__", sample == "demo" ? "StageBillingDemo" : "StageBillingMinA3", StringComparison.Ordinal));
        var probe = Probe.Replace("__PROJECT__", project, StringComparison.Ordinal)
            .Replace("__COMMAND_NAMESPACE__", NamespaceOf("RecordPayment.cs"), StringComparison.Ordinal)
            .Replace("__SCENARIO__", sample == "demo" ? DemoScenario : MinA3Scenario, StringComparison.Ordinal)
            .Replace("__OBSERVER_NAMESPACE__", NamespaceOf(sample == "demo" ? "AcknowledgeLargePayment.cs" : "PaymentSummaryReducer.cs"), StringComparison.Ordinal);
        AddGeneratedSpecification("InlineHttpProbe.cs", probe);
    }

    string NamespaceOf(string file) => ReadGeneratedFile(Assert.Single(GeneratedFiles(file)))
        .Split('\n').Single(line => line.StartsWith("namespace ", StringComparison.Ordinal))["namespace ".Length..].TrimEnd(';', '\r');

    async Task Execute(string sample, string project)
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        Assert.False(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION")));
        var failures = new List<Exception>();
        try
        {
            await Compile(sample, project);
            var name = project + ".KernelProbe.InlineHttpProbe.should_invoke_generated_inline_code";
            await Run(
                "inline-live.log",
                "test",
                project + ".csproj",
                "-c",
                "Debug",
                "--no-build",
                "--no-restore",
                "--filter",
                "FullyQualifiedName=" + name,
                "--logger",
                "trx;LogFileName=inline-probe.trx",
                "--results-directory",
                _evidence.FullName,
                "--nologo");
            RequireExecutedProbe(name);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        try { PreserveEvidence(sample, failures.Count == 0); }
        catch (Exception exception) { failures.Add(exception); }
        try { Cleanup(); }
        catch (Exception exception) { failures.Add(exception); }
        if (failures.Count > 0) throw new AggregateException(failures);
    }

    void PreserveEvidence(string sample, bool succeeded)
    {
        var counts = GeneratedFiles("inline-http-requests.json");
        if (succeeded || counts.Length > 0)
        {
            File.WriteAllText(Path.Combine(_evidence.FullName, "inline-http-requests.json"), ReadGeneratedFile(Assert.Single(counts)));
        }
        var destination = Directory.CreateDirectory(Path.Combine(_evidence.Parent.FullName, "inline-code", sample, _evidence.Name));
        foreach (var file in _evidence.GetFiles()) file.CopyTo(Path.Combine(destination.FullName, file.Name));
    }

    void RequireExecutedProbe(string name)
    {
        var document = XDocument.Load(Path.Combine(_evidence.FullName, "inline-probe.trx"));
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var result = Assert.Single(document.Descendants(ns + "UnitTestResult"));
        Assert.Equal(name, (string?)result.Attribute("testName"));
        Assert.Equal("Passed", (string?)result.Attribute("outcome"));
        var counters = Assert.Single(document.Descendants(ns + "Counters"));
        Assert.Equal("1", (string?)counters.Attribute("executed"));
        Assert.Equal("1", (string?)counters.Attribute("passed"));
        Assert.Equal("1", (string?)counters.Attribute("total"));
        Assert.Equal("0", (string?)counters.Attribute("notExecuted"));
    }

    const string HostSupport = """
        public partial class Program
        {
            public static readonly TaskCompletionSource<WebApplication> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public static WebApplication? ProbeApplication;

            static partial void ConfigureServices(WebApplicationBuilder builder)
            {
                builder.Configuration["Cratis:Chronicle:ConnectionString"] = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
                builder.Configuration["Cratis:Chronicle:EventStore"] = "__STORE__" + Guid.NewGuid().ToString("N");
                Console.WriteLine($"Inline fixture store: {builder.Configuration["Cratis:Chronicle:EventStore"]}");
                // Test-only forwarded identities; these unsigned headers are not production authentication.
                builder.Configuration["Cratis:Arc:TrustForwardedIdentityHeaders"] = "true";
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

    const string Probe = """
        using System.Net.Http.Json;
        using System.Reflection;
        using System.Text.Json;
        using Cratis.Chronicle;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Registrations;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.Hosting.Server;
        using Microsoft.AspNetCore.Hosting.Server.Features;
        using Microsoft.Extensions.DependencyInjection;
        using __COMMAND_NAMESPACE__;
        using __OBSERVER_NAMESPACE__;
        using Xunit;

        namespace __PROJECT__.KernelProbe;

        public class InlineHttpProbe
        {
            [Fact]
            public async Task should_invoke_generated_inline_code()
            {
                // Reserve 10 seconds each for stop and entry completion, and 15 for disposal.
                using var overall = new CancellationTokenSource(TimeSpan.FromSeconds(240));
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(overall.Token);
                deadline.CancelAfter(TimeSpan.FromSeconds(195));
                var failures = new List<Exception>();
                var entry = typeof(Program).GetMethod("<Main>$", BindingFlags.Static | BindingFlags.NonPublic)!;
                var running = (Task)entry.Invoke(null, [Array.Empty<string>()])!;
                WebApplication? app = null;
                InlineExercise? exercise = null;
                try
                {
                    Console.WriteLine("Inline phase: startup (60s)");
                    using var startup = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
                    startup.CancelAfter(TimeSpan.FromSeconds(60));
                    var first = await Task.WhenAny(Program.Started.Task, running).WaitAsync(startup.Token);
                    if (first == running) await running;
                    Assert.False(running.IsCompleted, "Host entry completed before the exercise.");
                    app = await Program.Started.Task.WaitAsync(startup.Token);
                    using var scope = app.Services.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<IEventStore>();
                    Console.WriteLine("Inline phase: registration (90s)");
                    var registration = await store.WaitForRegistration(TimeSpan.FromSeconds(90)).WaitAsync(deadline.Token);
                    Assert.True(registration.IsSuccess, $"Registration failed: {registration.Failure}");
                    using var client = LoopbackClient(app);
                    exercise = new InlineExercise(store, client, deadline.Token);
                    await exercise.Run();
                    Assert.False(running.IsCompleted, "Host entry completed during the exercise.");
                }
                catch (Exception exception) { failures.Add(exception); }
                try { exercise?.SaveRequestCounts(); }
                catch (Exception exception) { failures.Add(exception); }
                try { await Stop(app ?? Program.ProbeApplication, running, overall.Token); }
                catch (Exception exception) { failures.Add(exception); }
                if (failures.Count > 0) throw new AggregateException(failures);
            }

            static HttpClient LoopbackClient(WebApplication app)
            {
                // Verify the actual server listener, not merely the requested URLs, before sending any identity.
                var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
                var address = new Uri(Assert.Single(addresses!.Addresses));
                Assert.Equal("http", address.Scheme);
                Assert.Equal("127.0.0.1", address.Host);
                Assert.True(address.IsLoopback && address.Port > 0);
                return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = address, Timeout = TimeSpan.FromSeconds(10) };
            }

            static async Task Stop(WebApplication? app, Task running, CancellationToken overall)
            {
                Console.WriteLine("Inline phase: teardown");
                var failures = new List<Exception>();
                using var shutdown = CancellationTokenSource.CreateLinkedTokenSource(overall);
                shutdown.CancelAfter(TimeSpan.FromSeconds(10));
                try { if (app is not null) await app.StopAsync(shutdown.Token).WaitAsync(shutdown.Token); }
                catch (Exception exception) { failures.Add(exception); }
                using var completion = CancellationTokenSource.CreateLinkedTokenSource(overall);
                completion.CancelAfter(TimeSpan.FromSeconds(10));
                try { await running.WaitAsync(completion.Token); }
                catch (Exception exception) { failures.Add(exception); }
                using var disposal = CancellationTokenSource.CreateLinkedTokenSource(overall);
                disposal.CancelAfter(TimeSpan.FromSeconds(15));
                try { if (app is not null) await app.DisposeAsync().AsTask().WaitAsync(disposal.Token); }
                catch (Exception exception) { failures.Add(exception); }
                if (failures.Count > 0) throw new AggregateException(failures);
            }
        }

        sealed class InlineExercise(IEventStore store, HttpClient client, CancellationToken cancellation)
        {
            static readonly Guid Invoice = Guid.Parse("00000000-0000-0000-0000-000000999999");
            static readonly Guid Below = Guid.Parse("00000000-0000-0000-0000-000000000999");
            static readonly Guid Boundary = Guid.Parse("00000000-0000-0000-0000-000000001000");
            static readonly Guid Above = Guid.Parse("00000000-0000-0000-0000-000000001500");
            static readonly Guid Denied = Guid.Parse("00000000-0000-0000-0000-000000000010");
            static readonly Guid Negative = Guid.Parse("00000000-0000-0000-0000-000000000011");
            static readonly Guid Invalid = Guid.Parse("00000000-0000-0000-0000-000000000012");
            int _commands;
            int _keyedQueries;
            int _performerQueries = 0;

            __SCENARIO__

            async Task<JsonElement> Command(Guid id, decimal amount, string? department, int expectedStatus)
            {
                _commands++;
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/ledger/payments/record-payment")
                {
                    // The pinned UUID converter accepts JSON strings; Money's decimal converter accepts JSON numbers.
                    Content = JsonContent.Create(new { paymentId = id.ToString("D"), invoiceId = Invoice.ToString("D"), amount })
                };
                SetIdentity(request, department);
                return await Send(request, expectedStatus, cancellation);
            }

            static void SetIdentity(HttpRequestMessage request, string? department)
            {
                const string id = "synthetic-inline-probe";
                const string name = "Synthetic Inline Probe";
                var claims = new List<object>();
                if (department is not null) claims.Add(new { typ = "department", val = department });
                var principal = new { identityProvider = "synthetic", userId = id, userDetails = name, userRoles = Array.Empty<string>(), claims };
                request.Headers.Add("x-ms-client-principal", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(principal))));
                request.Headers.Add("x-ms-client-principal-id", id);
                request.Headers.Add("x-ms-client-principal-name", name);
            }

            async Task<JsonElement> Send(HttpRequestMessage request, int expectedStatus, CancellationToken token)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                using var response = await client.SendAsync(request, timeout.Token);
                var body = await response.Content.ReadAsStringAsync(timeout.Token);
                Console.WriteLine($"{request.Method} {request.RequestUri}: {(int)response.StatusCode} {body}");
                Assert.Equal(expectedStatus, (int)response.StatusCode);
                using var document = JsonDocument.Parse(body);
                Assert.False(document.RootElement.GetProperty("hasExceptions").GetBoolean(), body);
                return document.RootElement.Clone();
            }

            static void Accepted(JsonElement reply)
            {
                Assert.True(reply.GetProperty("isSuccess").GetBoolean(), reply.ToString());
                Assert.True(reply.GetProperty("isAuthorized").GetBoolean(), reply.ToString());
                Assert.True(reply.GetProperty("isValid").GetBoolean(), reply.ToString());
            }

            async Task<AppendedEvent[]> Facts(Guid id) =>
                (await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, id.ToString("D")).WaitAsync(cancellation)).ToArray();

            async Task Inventory(params (Guid Id, Type Type)[] expected)
            {
                var facts = (await store.EventLog.GetFromSequenceNumber(EventSequenceNumber.First).WaitAsync(cancellation)).ToArray();
                Assert.Equal(expected.Length, facts.Length);
                foreach (var item in expected)
                {
                    Assert.Single(facts, fact => fact.Context.EventSourceId.Value == item.Id.ToString("D") && fact.Content.GetType() == item.Type);
                }
            }

            async Task<AppendedEvent> Recorded(Guid id, decimal amount, bool settles)
            {
                var fact = Assert.Single(await Facts(id), fact => fact.Content is PaymentRecorded);
                var payment = Assert.IsType<PaymentRecorded>(fact.Content);
                Assert.Equal(id, payment.PaymentId.Value);
                Assert.Equal(Invoice, payment.InvoiceId.Value);
                Assert.Equal(amount, payment.Amount.Value);
                Assert.Equal(settles, payment.SettlesInvoice);
                Assert.Equal(id.ToString("D"), fact.Context.EventSourceId.Value);
                return fact;
            }

            async Task<JsonElement> Snapshot(Guid id, CancellationToken token)
            {
                _keyedQueries++;
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/ledger/payments/payment-list/payment-by-id?paymentId=" + id.ToString("D"));
                SetIdentity(request, "FinanceOps");
                var reply = await Send(request, 200, token);
                Assert.True(reply.GetProperty("isSuccess").GetBoolean(), reply.ToString());
                return reply.TryGetProperty("data", out var data) ? data.Clone() : default;
            }

            static void Row(JsonElement data, Guid id, decimal amount)
            {
                Assert.Equal(JsonValueKind.Object, data.ValueKind);
                Assert.Equal(id, data.GetProperty("paymentId").GetGuid());
                Assert.Equal(Invoice, data.GetProperty("invoiceId").GetGuid());
                Assert.Equal(amount, data.GetProperty("amount").GetDecimal());
            }

            public void SaveRequestCounts()
            {
                var counts = JsonSerializer.Serialize(new
                {
                    Commands = _commands,
                    KeyedQueries = _keyedQueries,
                    PerformerQueries = _performerQueries,
                    Total = _commands + _keyedQueries + _performerQueries
                });
                Console.WriteLine($"Inline HTTP counts: {counts}");
                File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "inline-http-requests.json"), counts);
            }
        }
        """;

    const string DemoScenario = """
        public async Task Run()
        {
            Accepted(await Command(Below, 999m, "FinanceOps", 200));
            Accepted(await Command(Boundary, 1000m, "FinanceOps", 200));
            Accepted(await Command(Above, 1500m, "FinanceOps", 200));
            await DeniedCommand();
            await Reactions(Below, 999m, false);
            await Reactions(Boundary, 1000m, false);
            await Reactions(Above, 1500m, true);
            Assert.Empty(await Facts(Denied));
            Assert.Empty(await store.Reactors.GetFailedPartitionsFor<AcknowledgeLargePayment>().WaitAsync(cancellation));
            await Inventory((Below, typeof(PaymentRecorded)), (Boundary, typeof(PaymentRecorded)), (Above, typeof(PaymentRecorded)), (Above, typeof(PaymentAcknowledged)));
            await ReadyAndPerform();
            Assert.Equal(4, _commands);
            Assert.Equal(1, _performerQueries);
        }

        async Task DeniedCommand()
        {
            var reply = await Command(Denied, 1500m, "Sales", 403);
            Assert.False(reply.GetProperty("isSuccess").GetBoolean(), reply.ToString());
            Assert.False(reply.GetProperty("isAuthorized").GetBoolean(), reply.ToString());
            Assert.Empty(await Facts(Denied));
        }

        async Task Reactions(Guid id, decimal amount, bool acknowledges)
        {
            var original = await Recorded(id, amount, amount >= 1000m);
            await store.Reactors.WaitTillReachesEventSequenceNumber<AcknowledgeLargePayment>(original.Context.SequenceNumber, TimeSpan.FromSeconds(60)).WaitAsync(cancellation);
            var facts = await Facts(id);
            Assert.Equal(acknowledges ? 2 : 1, facts.Length);
            var acknowledgements = facts.Where(fact => fact.Content is PaymentAcknowledged).ToArray();
            if (!acknowledges) { Assert.Empty(acknowledgements); return; }
            var fact = Assert.Single(acknowledgements);
            var acknowledged = Assert.IsType<PaymentAcknowledged>(fact.Content);
            Assert.Equal(id, acknowledged.PaymentId.Value);
            Assert.Equal(amount, acknowledged.Amount.Value);
            Assert.Equal(id.ToString("D"), fact.Context.EventSourceId.Value);
        }

        async Task ReadyAndPerform()
        {
            var expected = new[] { (Id: Above, Amount: 1500m), (Id: Boundary, Amount: 1000m), (Id: Below, Amount: 999m) };
            var pending = expected.ToList();
            using var readiness = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            readiness.CancelAfter(TimeSpan.FromSeconds(60));
            while (pending.Count > 0)
            {
                foreach (var payment in pending.ToArray())
                {
                    var data = await Snapshot(payment.Id, readiness.Token);
                    if (data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) continue;
                    Row(data, payment.Id, payment.Amount);
                    pending.Remove(payment);
                }
                if (pending.Count > 0) await Task.Delay(TimeSpan.FromMilliseconds(100), readiness.Token);
            }
            _performerQueries++;
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/ledger/payments/payment-list/largest-payments");
            SetIdentity(request, "FinanceOps");
            var reply = await Send(request, 200, cancellation);
            Assert.True(reply.GetProperty("isSuccess").GetBoolean(), reply.ToString());
            var rows = reply.GetProperty("data");
            Assert.Equal(JsonValueKind.Array, rows.ValueKind);
            Assert.Equal(3, rows.GetArrayLength());
            for (var index = 0; index < expected.Length; index++) Row(rows[index], expected[index].Id, expected[index].Amount);
            // Three rows prove invocation and sorting, not the >5 truncation boundary of Take(5).
        }
        """;

    const string MinA3Scenario = """
        public async Task Run()
        {
            await InvalidCommand(Invalid, 0m);
            await InvalidCommand(Negative, -1m);
            Accepted(await Command(Above, 1500m, null, 200));
            var recorded = await Recorded(Above, 1500m, true);
            Assert.Single(await Facts(Above));
            await store.Reducers.WaitTillReachesEventSequenceNumber<PaymentSummaryReducer>(recorded.Context.SequenceNumber, TimeSpan.FromSeconds(60)).WaitAsync(cancellation);
            Assert.Empty(await Facts(Invalid));
            Assert.Empty(await Facts(Negative));
            Assert.Empty(await store.Reducers.GetFailedPartitionsFor<PaymentSummaryReducer>().WaitAsync(cancellation));
            await Inventory((Above, typeof(PaymentRecorded)));
            Row(await Snapshot(Above, cancellation), Above, 1500m);
            Assert.Equal(3, _commands);
            Assert.Equal(1, _keyedQueries);
        }

        async Task InvalidCommand(Guid id, decimal amount)
        {
            var rejected = await Command(id, amount, null, 400);
            Assert.False(rejected.GetProperty("isSuccess").GetBoolean(), rejected.ToString());
            Assert.True(rejected.GetProperty("isAuthorized").GetBoolean(), rejected.ToString());
            Assert.False(rejected.GetProperty("isValid").GetBoolean(), rejected.ToString());
            Assert.Contains(rejected.GetProperty("validationResults").EnumerateArray(), result => result.GetProperty("message").GetString() == "A payment has to be for something");
            Assert.Empty(await Facts(id));
        }
        """;
}
#endif
