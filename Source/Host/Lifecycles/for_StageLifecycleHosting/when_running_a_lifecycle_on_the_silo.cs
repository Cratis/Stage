// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Orleans.Hosting;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Lifecycles.for_LifecycleAdmission.given;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Lifecycles;
using Cratis.Stage.Semantics;
using Orleans.Configuration;
using Orleans.Hosting;
using Xunit;

namespace Cratis.Stage.Host.Lifecycles.for_StageLifecycleHosting;

/// <summary>
/// Runs the invoice lifecycle end to end: the semantic runtime commits facts, the state machine enters Sent, and the Collect
/// workflow runs on a real Orleans silo with in-memory storage until it waits for a payment or an approval.
/// </summary>
public class when_running_a_lifecycle_on_the_silo
{
    static readonly string _fast = a_lifecycle_source.Source.Replace("wait Pause for 2 days", "wait Pause for 1 second", StringComparison.Ordinal);

    [Fact]
    public async Task should_send_the_commands_run_the_workflow_and_resume_it_with_the_payment()
    {
        await using var lifecycle = await Start(_fast);
        await lifecycle.Execute("RegisterInvoice", Register);
        await lifecycle.Execute("MarkInvoiceOverdue");
        await lifecycle.Execute("SendInvoice");

        // Entering Sent sends ChargeFee and starts Collect, whose first action sends ChargeFee again; then a second passes,
        // the overdue invoice is chased once per line, and the run waits for the payment.
        await lifecycle.Eventually(() => lifecycle.Count("InvoiceTagged") == 2 && lifecycle.Count("LateFeeCharged") == 2);
        await lifecycle.Eventually(async () => (await lifecycle.WaitingAt("Collect")).Contains("Await"));
        Assert.Equal(["sent"], [.. await lifecycle.States()]);

        await lifecycle.Execute("PayInvoice");
        await lifecycle.Eventually(() => lifecycle.Count("InvoiceTagged") == 3);
        Assert.Equal(0, lifecycle.Count("InvoiceWrittenOff"));
    }

    [Fact]
    public async Task should_write_the_invoice_off_when_the_payment_never_comes_and_the_approval_is_given()
    {
        await using var lifecycle = await Start(_fast.Replace("expires after 7 days", "expires after 1 second", StringComparison.Ordinal));
        await lifecycle.Execute("RegisterInvoice", Register);
        await lifecycle.Execute("MarkInvoiceOverdue");
        await lifecycle.Execute("SendInvoice");

        await lifecycle.Eventually(async () => (await lifecycle.WaitingAt("Collect")).Contains("Approve"));
        Assert.True(await lifecycle.Decide("Approve", approved: true));
        await lifecycle.Eventually(() => lifecycle.Count("InvoiceWrittenOff") == 1);
    }

    static void Register(Dictionary<string, JsonElement> payload) =>
        payload["lines"] = JsonSerializer.SerializeToElement(new[] { new { sku = "A" }, new { sku = "B" } });

    static async Task<Lifecycle> Start(string source)
    {
        var plan = a_lifecycle_source.Plan(source);
        var log = new a_semantic_fact_log();
        var runtime = log.Runtime(plan, SemanticWorld.Empty);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ISemanticRuntime>(runtime);
        StageLifecycleHosting.Add(builder.Services, plan);
        var siloPort = FreePort();
        builder.AddCratisOrleans(new CratisOrleansOptions { Clustering = ClusteringMode.Localhost, Storage = GrainStorageMode.InMemory, ClusterId = "lifecycle-spec", ServiceId = "lifecycle-spec", Workflows = true });
        builder.Services.Configure<EndpointOptions>(options =>
        {
            options.SiloPort = siloPort;
            options.GatewayPort = FreePort();
        });
        builder.Services.Configure<DevelopmentClusterMembershipOptions>(options => options.PrimarySiloEndpoint = new IPEndPoint(IPAddress.Loopback, siloPort));
        var app = builder.Build();
        await app.StartAsync();
        var driver = await StageLifecycleHosting.Start(app.Services, plan);
        return new(app, runtime, log, plan, driver!);
    }

    static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    sealed class Lifecycle(WebApplication app, SemanticRuntime runtime, a_semantic_fact_log log, SemanticExecutionPlan plan, LifecycleDriver driver) : IAsyncDisposable
    {
        const string Invoice = a_lifecycle_source.InvoiceId;

        public int Count(string @event) => log.Facts.Count(fact => plan.Events[fact.EventContract].Name == @event);

        public async Task Execute(string command, Action<Dictionary<string, JsonElement>>? configure = null)
        {
            var payload = new Dictionary<string, JsonElement> { ["invoiceId"] = JsonSerializer.SerializeToElement(Invoice) };
            configure?.Invoke(payload);
            var semantic = plan.Commands.Values.Single(candidate => candidate.Name == command);
            var result = await runtime.Execute(semantic, payload, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "spec", "spec", "spec"), false);
            Assert.IsType<SemanticAccepted>(result);
            await driver.Pump();
        }

        public async Task<IReadOnlyList<string>> WaitingAt(string workflow) =>
            [.. (await Effects.Runs(workflow)).SelectMany(run => run.WaitingAt)];

        public async Task<IReadOnlyList<string>> States() =>
            [.. (await driver.States(plan.Model.Application.StateMachines.Single())).Select(item => item.State.ToLowerInvariant())];

        public Task<bool> Decide(string node, bool approved) => Effects.Decide("Collect", Invoice, node, approved, "spec");

        public async Task Eventually(Func<bool> condition) => await Eventually(() => Task.FromResult(condition()));

        public async Task Eventually(Func<Task<bool>> condition)
        {
            var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
            while (!await condition())
            {
                Assert.True(DateTimeOffset.UtcNow < deadline, $"Timed out. Facts: {string.Join(", ", log.Facts.Select(fact => plan.Events[fact.EventContract].Name))}. Runs: {string.Join(" | ", (await Effects.Runs("Collect")).Select(run => $"{run.Status}: {string.Join(", ", run.Nodes)}"))}");
                await Task.Delay(100);
                await driver.Pump();
            }
        }

        StageLifecycleEffects Effects => app.Services.GetRequiredService<StageLifecycleEffects>();

        public async ValueTask DisposeAsync()
        {
            driver.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
