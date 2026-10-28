// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Stage.Rendering.Kits.given;

static class kit
{
    internal const string Manifest = """
        {"apiVersion":"stage-target/v1.0","target":{"id":"axon","version":"1.0.0"},
        "languagePacks":["java@1.0.0"],"requires":["model/v1.0"],
        "outputs":[{"id":"main","template":"templates/main.sbn","path":"{{ name }}.java"}],
        "partials":[],"tables":[],"assets":[],"diagnostics":[]}
        """;

    internal static KitEngineCapabilities Capabilities { get; } = new(0, [new("java", "1.0.0")], [new("model", 1, 1)]);

    internal static ImmutableArray<byte> Bytes(string text) => [.. Encoding.UTF8.GetBytes(text)];

    internal static Dictionary<string, ImmutableArray<byte>> Files(string? manifest = null) => new(StringComparer.Ordinal)
    {
        [KitLoader.ManifestPath] = Bytes(manifest ?? Manifest),
        ["templates/main.sbn"] = Bytes("hello")
    };

    internal static KitLoadResult Load(string manifest) => KitLoader.Load(new InMemoryKitSource(Files(manifest)), Capabilities);

    internal static string Change(string path, JsonNode? value)
    {
        var root = JsonNode.Parse(Manifest)!;
        root[path] = value;
        return root.ToJsonString();
    }
}
