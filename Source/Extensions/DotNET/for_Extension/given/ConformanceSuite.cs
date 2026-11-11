// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Stage.Extensions.Protocol;
using Google.Protobuf;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Stage.Extensions.for_Extension.given;

/// <summary>
/// Runs the shared conformance cases against an extension, over the wire, with a fake host.
/// </summary>
public static class ConformanceSuite
{
    public static async Task<(int Cases, IReadOnlyList<string> Failures)> Run(ExtensionDefinition definition)
    {
        var document = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "cases.json")))!;
        var token = document["token"]!.GetValue<string>();
        var settings = document["host"]!["settings"]!.AsObject().ToDictionary(pair => pair.Key, pair => pair.Value!.GetValue<string>());
        var secrets = document["host"]!["secrets"]!.AsObject().ToDictionary(pair => pair.Key, pair => pair.Value!.GetValue<string>());

        var fake = new FakeHost(settings, secrets);
        await using var host = await Serve(builder => builder.Services.AddSingleton(fake), app => app.MapGrpcService<FakeHost>());
        await using var extension = ExtensionRunner.Build(definition, new IPEndPoint(IPAddress.Loopback, 0));
        await extension.StartAsync();
        using var channel = GrpcChannel.ForAddress(extension.Urls.First());
        var client = new ExtensionBackend.ExtensionBackendClient(channel);

        var failures = new List<string>();
        var cases = document["cases"]!.AsArray();
        foreach (var node in cases)
        {
            var name = node!["name"]!.GetValue<string>();
            try
            {
                while (fake.Calls.TryDequeue(out _))
                {
                }

                var request = node["request"]!.ToJsonString().Replace("@host", host.Urls.First(), StringComparison.Ordinal);
                var actual = node["rpc"]!.GetValue<string>() switch
                {
                    "Describe" => Format(await client.DescribeAsync(JsonParser.Default.Parse<DescribeRequest>(request))),
                    "Invoke" => Format(await client.InvokeAsync(JsonParser.Default.Parse<InvokeRequest>(request))),
                    "HandleHttp" => Format(await client.HandleHttpAsync(JsonParser.Default.Parse<InboundHttpRequest>(request))),
                    "Health" => Format(await client.HealthAsync(JsonParser.Default.Parse<HealthRequest>(request))),
                    var rpc => throw new InvalidOperationException($"Unknown rpc {rpc}.")
                };
                Subset(node["expect"], actual, "response", failures, name);

                if (node["hostCalls"] is JsonArray expectedCalls)
                {
                    var deadline = DateTime.UtcNow.AddSeconds(5);
                    while (fake.Calls.Count < expectedCalls.Count && DateTime.UtcNow < deadline) await Task.Delay(20);
                    var calls = fake.Calls.ToArray();
                    if (calls.Length != expectedCalls.Count)
                    {
                        failures.Add($"{name}: expected {expectedCalls.Count} host calls, saw {calls.Length}.");
                        continue;
                    }

                    for (var index = 0; index < calls.Length; index++)
                    {
                        var expected = expectedCalls[index]!;
                        if (calls[index].Rpc != expected["rpc"]!.GetValue<string>()) failures.Add($"{name}: host call {index} was {calls[index].Rpc}, expected {expected["rpc"]}.");
                        if (calls[index].Authorization != $"Bearer {token}") failures.Add($"{name}: host call {index} did not present the session credential.");
                        Subset(expected["request"], JsonNode.Parse(JsonFormatter.Default.Format(calls[index].Request)), $"host call {index}", failures, name);
                    }
                }
            }
            catch (Exception exception)
            {
                failures.Add($"{name}: {exception.Message}");
            }
        }

        return (cases.Count, failures);
    }

    static JsonNode? Format(IMessage message) => JsonNode.Parse(JsonFormatter.Default.Format(message));

    static void Subset(JsonNode? expected, JsonNode? actual, string path, List<string> failures, string name)
    {
        switch (expected)
        {
            case JsonObject expectedObject:
                if (actual is not JsonObject actualObject)
                {
                    failures.Add($"{name}: {path} is not an object.");
                    return;
                }

                foreach (var (key, value) in expectedObject) Subset(value, actualObject[key], $"{path}.{key}", failures, name);
                break;
            case JsonArray expectedArray:
                if (actual is not JsonArray actualArray || actualArray.Count != expectedArray.Count)
                {
                    failures.Add($"{name}: {path} does not have {expectedArray.Count} items.");
                    return;
                }

                for (var index = 0; index < expectedArray.Count; index++) Subset(expectedArray[index], actualArray[index], $"{path}[{index}]", failures, name);
                break;
            default:
                // A field that holds JSON (its name ends in Json) is compared as JSON, so key order does not matter.
                if (path.EndsWith("Json", StringComparison.Ordinal) && expected is JsonValue wanted && actual is JsonValue got && wanted.TryGetValue<string>(out var wantedText) && got.TryGetValue<string>(out var gotText))
                {
                    expected = JsonNode.Parse(wantedText);
                    actual = JsonNode.Parse(gotText);
                }

                if (actual is null || !JsonNode.DeepEquals(expected, actual)) failures.Add($"{name}: {path} was {actual?.ToJsonString() ?? "missing"}, expected {expected?.ToJsonString()}.");
                break;
        }
    }

    static async Task<WebApplication> Serve(Action<WebApplicationBuilder> configure, Action<WebApplication> map)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http2));
        builder.Services.AddRouting();
        builder.Services.AddGrpc();
        configure(builder);
        var app = builder.Build();
        app.UseRouting();
        map(app);
        await app.StartAsync();
        return app;
    }
}
