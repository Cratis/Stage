// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_CratisServiceConfiguration;

public class when_encrypting_against_the_pinned_kernel
{
    [Fact]
    public async Task should_encrypt_and_release_concepts_when_configured()
    {
        var address = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_MONGO_CONNECTION");
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        var name = $"StageCompliance{Guid.NewGuid():N}";
        var builder = WebApplication.CreateBuilder();
        builder.AddStageCratis(name, "Stage compliance probe");
        builder.Services.PostConfigure<ChronicleAspNetCoreOptions>(options => options.ConnectionString = address);
        await using var app = builder.Build();
        var client = app.Services.GetRequiredService<IChronicleClient>();
        var kernelStore = new KernelStore(await KernelContainer(address), name);
        await VerifyRequiredProperties(client, kernelStore);
        var failures = new List<Exception>();
        foreach (var testCase in new ProtectedCase[]
        {
            new("pii", "String", true, "optional-personal"),
            new("secret", "String", true, "optional-secret"),
            new("pii", "String", true, null),
            new("secret", "String", true, null)
        })
        {
            try
            {
                await VerifySingleProtectedProperty(client, kernelStore, testCase);
            }
            catch (Exception error)
            {
                // Collect diagnostic cases, but never turn a partial probe into a passing result.
                Console.WriteLine($"FAILED {testCase}: {error}");
                failures.Add(error);
            }
        }
        Assert.Empty(failures);
    }

    static async Task VerifyRequiredProperties(IChronicleClient client, KernelStore kernelStore)
    {
        var name = kernelStore.Name;
        var plan = compiled_plan.From(a_compliance_store.Source);
        Assert.Empty(new SemanticRuntimeAdmission(plan).Blocking);
        var nonTextSecret = compiled_plan.From(a_compliance_store.Source.Replace("AccessToken : String secret", "AccessToken : Int secret", StringComparison.Ordinal));
        Assert.StartsWith("STAGE-ESM-041:", Assert.Single(new SemanticRuntimeAdmission(nonTextSecret).Blocking).Details);
        await SemanticChronicleRegistration.Register(client, name, plan);
        var appender = new SemanticFactAppender(client, new StageEventStoreName(name), plan);
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        var valuesToAppend = new Dictionary<string, JsonElement>
        {
            ["id"] = JsonSerializer.SerializeToElement("record-1"),
            ["name"] = JsonSerializer.SerializeToElement("Synthetic Name"),
            ["number"] = JsonSerializer.SerializeToElement(37),
            ["token"] = JsonSerializer.SerializeToElement("synthetic-secret")
        };
        var result = await runtime.Execute(plan.Commands.Values.Single(), valuesToAppend, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "caller", "Caller", "user"), false);
        Assert.IsType<SemanticAccepted>(result);
        using var raw = await StoredContent(kernelStore);
        foreach (var property in new[] { "name", "number", "token" })
        {
            AssertCiphertext(raw.RootElement.GetProperty(property), valuesToAppend[property]);
        }
        var store = await client.GetEventStore(name);
        var accessor = (IChronicleServicesAccessor)store.Connection;
        using var released = await ReleasedContent(accessor, kernelStore);
        Assert.Equal("Synthetic Name", released.RootElement.GetProperty("name").GetString());
        Assert.Equal(37, released.RootElement.GetProperty("number").GetInt32());
        Assert.Equal("synthetic-secret", released.RootElement.GetProperty("token").GetString());
        var values = await RebuiltValues(accessor, kernelStore, plan, appender);
        foreach (var property in new[] { "name", "number", "token" })
        {
            AssertRebuiltValue(plan, values, property, valuesToAppend[property]);
        }
        Console.WriteLine("Raw name, number and token are nonempty ciphertext strings; released and rebuilt values are Synthetic Name, 37 and synthetic-secret.");
    }

    static async Task VerifySingleProtectedProperty(IChronicleClient client, KernelStore kernelStore, ProtectedCase testCase)
    {
        var name = $"StageProtect{Guid.NewGuid():N}";
        var protectedStore = kernelStore with { Name = name };
        var plan = ProtectedPlan(testCase);
        Assert.Empty(new SemanticRuntimeAdmission(plan).Blocking);
        await SemanticChronicleRegistration.Register(client, name, plan);
        var appender = new SemanticFactAppender(client, new StageEventStoreName(name), plan);
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        var expected = JsonSerializer.SerializeToElement(testCase.Value);
        var payload = new Dictionary<string, JsonElement> { ["id"] = JsonSerializer.SerializeToElement("record-1"), ["value"] = expected };
        var result = await runtime.Execute(plan.Commands.Values.Single(), payload, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "caller", "Caller", "user"), false);
        Assert.True(result is SemanticAccepted, result.ToString());
        using var raw = await StoredContent(protectedStore);
        if (testCase.Value is null)
        {
            // Chronicle may omit an absent value from stored content; either form means nothing was stored.
            AssertAbsent(raw.RootElement);
        }
        else
        {
            AssertCiphertext(raw.RootElement.GetProperty("value"), expected);
        }
        var store = await client.GetEventStore(name);
        var accessor = (IChronicleServicesAccessor)store.Connection;
        using var released = await ReleasedContent(accessor, protectedStore);
        if (testCase.Value is null)
        {
            AssertAbsent(released.RootElement);
        }
        else
        {
            Assert.Equal(expected.GetRawText(), released.RootElement.GetProperty("value").GetRawText());
        }
        AssertRebuiltValue(plan, await RebuiltValues(accessor, protectedStore, plan, appender), "value", expected);
        Console.WriteLine($"PASS {testCase}: stored {(testCase.Value is null ? "null" : "ciphertext")}; released and rebuilt {expected.GetRawText()}.");
    }

    static SemanticExecutionPlan ProtectedPlan(ProtectedCase testCase)
    {
        var propertyType = $"Protected{(testCase.Optional ? "?" : string.Empty)}";

        return compiled_plan.From($"""
            concept Protected : {testCase.Primitive} {testCase.Classification}
            module Records
              feature Registration
                slice StateChange Register
                  command Register
                    id String identifier
                    value {propertyType}
                    produces Registered
                      for id
                      value = value
                  event Registered
                    value {propertyType}
                slice StateView Summary
                  readmodel Summary
                    recordId String
                    value {propertyType}
                  projection SummaryProjection => Summary
                    from Registered key $eventSourceId
                      recordId = $eventSourceId
                      value = value
                  query All => Summary[]
            """);
    }

    static async Task<string> KernelContainer(string address)
    {
        // The publish filter matches the container-side port, so locate the kernel by its host-side mapping instead.
        var hostPort = $":{new Uri(address).Port}->";

        return Assert.Single(
            (await Docker("ps", "--format", "{{.ID}} {{.Ports}}")).Split('\n', StringSplitOptions.RemoveEmptyEntries),
            line => line.Contains(hostPort, StringComparison.Ordinal)).Split(' ')[0];
    }

    static async Task<JsonDocument> StoredContent(KernelStore kernelStore)
    {
        // Read BSON directly inside the development container: Chronicle's sequence APIs release values.
        var script = $"const e = db.getSiblingDB({JsonSerializer.Serialize($"{kernelStore.Name}+es+Default")}).getCollection('event-log').findOne(); if (!e) throw new Error('Missing stored event'); print(JSON.stringify(e.content['1']));";

        return JsonDocument.Parse(await Docker("exec", kernelStore.Container, "mongosh", "--quiet", "--eval", script));
    }

    static async Task<JsonDocument> ReleasedContent(IChronicleServicesAccessor accessor, KernelStore kernelStore)
    {
        var events = (await accessor.Services.Sequences.FromSequenceNumber(new FromSequenceNumberRequest
        {
            EventStore = kernelStore.Name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log,
            FromEventSequenceNumber = 0
        })).EnsureSuccess();

        return JsonDocument.Parse(Assert.Single(events).Content);
    }

    static async Task<IReadOnlyList<SemanticPropertyValue>> RebuiltValues(IChronicleServicesAccessor accessor, KernelStore kernelStore, SemanticExecutionPlan plan, SemanticFactAppender appender)
    {
        var world = await SemanticChronicleRegistration.Rebuild(accessor, kernelStore.Name, plan, await appender.Tail());
        var rebuilt = SemanticRuntimeHosting.Create(plan, appender, world);
        var queried = Assert.IsType<SemanticAccepted>(await rebuilt.Query(plan.Queries.Values.Single(), SemanticValue.Null, new ClaimsPrincipal()));

        return Assert.Single(Assert.Single(queried.Queries).Results).Values;
    }

    static void AssertCiphertext(JsonElement actual, JsonElement expected)
    {
        Assert.Equal(JsonValueKind.String, actual.ValueKind);
        Assert.NotEmpty(actual.GetString()!);
        Assert.NotEqual(expected.ToString(), actual.GetString());
    }

    static void AssertRebuiltValue(SemanticExecutionPlan plan, IReadOnlyList<SemanticPropertyValue> values, string name, JsonElement expected)
    {
        var property = plan.ReadModels.Values.Single().Properties.Single(property => property.Name == name);
        var actual = values.Single(entry => entry.TargetProperty == property.Id).Value;
        Assert.Equal(SemanticJsonValues.Bind(expected, property.Type, plan.Model), actual);
    }

    static void AssertAbsent(JsonElement content) =>
        Assert.True(!content.TryGetProperty("value", out var value) || value.ValueKind == JsonValueKind.Null, content.GetRawText());

    static async Task<string> Docker(params string[] arguments)
    {
        var start = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(deadline.Token);
            var error = process.StandardError.ReadToEndAsync(deadline.Token);
            await process.WaitForExitAsync(deadline.Token);
            Assert.True(process.ExitCode == 0, await error);

            return await output;
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
        }
    }

    sealed record KernelStore(string Container, string Name);
    sealed record ProtectedCase(string Classification, string Primitive, bool Optional, object? Value);
}
