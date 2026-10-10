// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// The Stage frontend runtime a generated React application renders its Scene with.
/// </summary>
/// <remarks>
/// These are the live Stage frontend's own runtime modules, embedded when this renderer is built, so a generated
/// application and a live Stage render the same Scene through the same table, form, dialog, navigation and theme
/// code. The generated application only supplies where the Scene, routes and strings come from.
/// </remarks>
internal static class StageFrontendRuntime
{
    const string Prefix = "StageFrontend/";

    /// <summary>
    /// Gets every runtime file as its path in the generated application and its text, in ordinal path order.
    /// </summary>
    /// <remarks>
    /// Line endings are normalized to line feeds so the emitted bytes do not depend on how the renderer's own
    /// sources were checked out.
    /// </remarks>
    internal static IReadOnlyList<(string RelativePath, string Content)> Files { get; } = Load();

    static (string RelativePath, string Content)[] Load()
    {
        var assembly = typeof(StageFrontendRuntime).Assembly;
        var files = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(Prefix, StringComparison.Ordinal))
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream, new UTF8Encoding(false, true));
                var content = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
                return ($".frontend/{name[Prefix.Length..]}", content);
            })
            .OrderBy(file => file.Item1, StringComparer.Ordinal)
            .ToArray();

        return files.Length > 0
            ? files
            : throw new InvalidCratisBackendApplicationScaffold("The Stage frontend runtime is missing from the renderer.");
    }
}
