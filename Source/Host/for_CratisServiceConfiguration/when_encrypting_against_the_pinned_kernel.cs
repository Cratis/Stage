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
        var plan = compiled_plan.From(a_compliance_store.Source);
        Assert.Empty(new SemanticRuntimeAdmission(plan).Blocking);
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

        // Read BSON directly inside the development container: Chronicle's sequence APIs release values.
        // The publish filter matches the container-side port, so locate the kernel by its host-side mapping instead.
        var hostPort = $":{new Uri(address).Port}->";
        var container = Assert.Single(
            (await Docker("ps", "--format", "{{.ID}} {{.Ports}}")).Split('\n', StringSplitOptions.RemoveEmptyEntries),
            line => line.Contains(hostPort, StringComparison.Ordinal)).Split(' ')[0];
        var script = $"const e = db.getSiblingDB({JsonSerializer.Serialize($"{name}+es+Default")}).getCollection('event-log').findOne(); if (!e) throw new Error('Missing stored event'); print(JSON.stringify(e.content['1']));";
        using var raw = JsonDocument.Parse(await Docker("exec", container, "mongosh", "--quiet", "--eval", script));
        foreach (var property in new[] { "name", "number", "token" })
        {
            var value = raw.RootElement.GetProperty(property);
            Assert.Equal(JsonValueKind.String, value.ValueKind);
            Assert.NotEmpty(value.GetString()!);
        }
        Assert.NotEqual("Synthetic Name", raw.RootElement.GetProperty("name").GetString());
        Assert.NotEqual("37", raw.RootElement.GetProperty("number").GetString());
        Assert.NotEqual("synthetic-secret", raw.RootElement.GetProperty("token").GetString());

        var store = await client.GetEventStore(name);
        var accessor = (IChronicleServicesAccessor)store.Connection;
        var events = (await accessor.Services.Sequences.FromSequenceNumber(new FromSequenceNumberRequest
        {
            EventStore = name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log,
            FromEventSequenceNumber = 0
        })).EnsureSuccess();
        using var released = JsonDocument.Parse(Assert.Single(events).Content);
        Assert.Equal("Synthetic Name", released.RootElement.GetProperty("name").GetString());
        Assert.Equal(37, released.RootElement.GetProperty("number").GetInt32());
        Assert.Equal("synthetic-secret", released.RootElement.GetProperty("token").GetString());

        var world = await SemanticChronicleRegistration.Rebuild(accessor, name, plan, await appender.Tail());
        var rebuilt = SemanticRuntimeHosting.Create(plan, appender, world);
        var queried = Assert.IsType<SemanticAccepted>(await rebuilt.Query(plan.Queries.Values.Single(), SemanticValue.Null, new ClaimsPrincipal()));
        var values = Assert.Single(Assert.Single(queried.Queries).Results).Values;
        Assert.Equal("Synthetic Name", Assert.IsType<SemanticTextValue>(values.Single(value => value.TargetProperty == plan.ReadModels.Values.Single().Properties.Single(property => property.Name == "name").Id).Value).Value);
        Assert.Equal("synthetic-secret", Assert.IsType<SemanticTextValue>(values.Single(value => value.TargetProperty == plan.ReadModels.Values.Single().Properties.Single(property => property.Name == "token").Id).Value).Value);
        Assert.Equal(SemanticValue.Number(37), values.Single(value => value.TargetProperty == plan.ReadModels.Values.Single().Properties.Single(property => property.Name == "number").Id).Value);
        Console.WriteLine("Raw name, number and token are nonempty ciphertext strings; released and rebuilt values are Synthetic Name, 37 and synthetic-secret.");
        var failures = new List<Exception>();
        foreach (var (classification, primitive, optional, value) in new (string, string, bool, object)[]
        {
            ("pii", "String", true, "optional-personal"),
            ("secret", "String", true, "optional-secret"),
            ("secret", "Int", false, 41),
            ("secret", "Bool", false, true)
        })
        {
            try
            {
                await VerifySingleProtectedProperty(client, container, classification, primitive, optional, value);
            }
            catch (Exception error)
            {
                // Collect diagnostic cases, but never turn a partial probe into a passing result.
                Console.WriteLine($"FAILED {classification} {primitive} optional={optional}: {error}");
                failures.Add(error);
            }
        }
        Assert.Empty(failures);
    }

    static async Task VerifySingleProtectedProperty(IChronicleClient client, string container, string classification, string primitive, bool optional, object value)
    {
        var name = $"StageProtect{Guid.NewGuid():N}";
        var propertyType = $"Protected{(optional ? "?" : string.Empty)}";
        var plan = compiled_plan.From($"""
            concept Protected : {primitive} {classification}
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
        Assert.Empty(new SemanticRuntimeAdmission(plan).Blocking);
        await SemanticChronicleRegistration.Register(client, name, plan);
        var appender = new SemanticFactAppender(client, new StageEventStoreName(name), plan);
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        var expected = JsonSerializer.SerializeToElement(value);
        var payload = new Dictionary<string, JsonElement> { ["id"] = JsonSerializer.SerializeToElement("record-1"), ["value"] = expected };
        var result = await runtime.Execute(plan.Commands.Values.Single(), payload, new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "caller", "Caller", "user"), false);
        Assert.True(result is SemanticAccepted, result.ToString());
        var script = $"const e = db.getSiblingDB({JsonSerializer.Serialize($"{name}+es+Default")}).getCollection('event-log').findOne(); if (!e) throw new Error('Missing stored event'); print(JSON.stringify(e.content['1']));";
        using var raw = JsonDocument.Parse(await Docker("exec", container, "mongosh", "--quiet", "--eval", script));
        var ciphertext = raw.RootElement.GetProperty("value");
        Assert.Equal(JsonValueKind.String, ciphertext.ValueKind);
        Assert.NotEmpty(ciphertext.GetString()!);
        Assert.NotEqual(value.ToString(), ciphertext.GetString());
        var store = await client.GetEventStore(name);
        var accessor = (IChronicleServicesAccessor)store.Connection;
        var events = (await accessor.Services.Sequences.FromSequenceNumber(new FromSequenceNumberRequest
        {
            EventStore = name,
            Namespace = EventStoreNamespaceName.Default,
            EventSequenceId = EventSequenceId.Log,
            FromEventSequenceNumber = 0
        })).EnsureSuccess();
        using var released = JsonDocument.Parse(Assert.Single(events).Content);
        Assert.Equal(expected.GetRawText(), released.RootElement.GetProperty("value").GetRawText());
        var rebuilt = SemanticRuntimeHosting.Create(plan, appender, await SemanticChronicleRegistration.Rebuild(accessor, name, plan, await appender.Tail()));
        var queried = Assert.IsType<SemanticAccepted>(await rebuilt.Query(plan.Queries.Values.Single(), SemanticValue.Null, new ClaimsPrincipal()));
        var readModel = plan.ReadModels.Values.Single();
        var actual = Assert.Single(Assert.Single(queried.Queries).Results).Values.Single(entry => entry.TargetProperty == readModel.Properties.Single(property => property.Name == "value").Id).Value;
        Assert.Equal(SemanticJsonValues.Bind(expected, readModel.Properties.Single(property => property.Name == "value").Type, plan.Model), actual);
        Console.WriteLine($"PASS {classification} {primitive} optional={optional}: stored ciphertext; released and rebuilt {expected.GetRawText()}.");
    }

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
}
