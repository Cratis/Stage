// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration;

/// <summary>
/// Emits the same tenant translation source compiled into Stage's command runtime.
/// </summary>
internal static class TenantTranslationSource
{
    internal static RenderedFile Render(string rootNamespace)
    {
        var source = Read("PortableTenantValues.cs").Replace("namespace Cratis.Stage.Runtime;", $"namespace {rootNamespace}.GeneratedTenancy;", StringComparison.Ordinal) +
            Read("AmbiguousTenant.cs").Replace("namespace Cratis.Stage.Runtime;", string.Empty, StringComparison.Ordinal);

        return new(Path.Combine("GeneratedTenancy", "PortableTenantValues.cs"), source);
    }

    static string Read(string name)
    {
        using var resource = typeof(TenantTranslationSource).Assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(resource);

        // Stage's own license header stays with Stage; the generated copy belongs to the application.
        var lines = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var start = 0;
        while (start < lines.Length && lines[start].StartsWith("//", StringComparison.Ordinal)) start++;
        while (start < lines.Length && lines[start].Length == 0) start++;

        return string.Join('\n', lines[start..]);
    }
}
