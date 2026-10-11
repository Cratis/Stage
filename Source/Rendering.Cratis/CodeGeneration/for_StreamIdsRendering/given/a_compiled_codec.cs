// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_StreamIdsRendering.given;

public class a_compiled_codec : Specification
{
    protected Type _codec = null!;
    protected JsonDocument _vectors = null!;

    void Establish()
    {
        _codec = RenderedOutput.Load([StreamIdsRendering.Render("Codec")]).GetType("Codec.GeneratedEventSources.StreamIds")!;
        var assembly = typeof(a_compiled_codec).Assembly;
        using var stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(name => name.EndsWith("stream-id-codec.json", StringComparison.Ordinal)))!;
        _vectors = JsonDocument.Parse(stream);
    }

    protected object Invoke(string method, params object[] arguments) => _codec.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, arguments)!;

    protected bool Text(string value, out string formatted)
    {
        object[] arguments = [value, string.Empty];
        var valid = (bool)Invoke("TryText", arguments);
        formatted = (string)arguments[1];
        return valid;
    }

    protected static string Input(JsonElement vector) => vector.TryGetProperty("utf16", out var utf16)
        ? new string([.. utf16.EnumerateArray().Select(item => (char)Convert.ToInt32(item.GetString(), 16))])
        : vector.GetProperty("input").GetString()!;

    void Destroy() => _vectors.Dispose();
}
