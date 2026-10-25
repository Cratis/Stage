// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// Restores the Cratis license header generated C# carried before headers became the caller's choice, so earlier
/// frozen bytes remain a gate that the header was the only change.
/// </summary>
internal static class LegacyLicenseHeader
{
    internal const string Text = "// Copyright (c) Cratis. All rights reserved.\n// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\n";

    internal static string Restore(string relativePath, string content)
    {
        if (!relativePath.EndsWith(".cs", StringComparison.Ordinal) || content.StartsWith(Text, StringComparison.Ordinal)) return content;

        // The tenant translation source concatenates two Stage runtime files, each of which carried the header.
        var ambiguous = content.IndexOf("public sealed class AmbiguousTenant", StringComparison.Ordinal);
        if (relativePath.EndsWith("GeneratedTenancy/PortableTenantValues.cs", StringComparison.Ordinal) && ambiguous >= 0)
        {
            var second = content.LastIndexOf("\n\n/// <summary>", ambiguous, StringComparison.Ordinal);
            content = content[..second] + Text + content[second..];
        }

        return Text + content;
    }

    internal static IEnumerable<PlannedArtifact> Restore(IEnumerable<PlannedArtifact> artifacts) => artifacts.Select(artifact =>
        artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)
            ? PlannedArtifact.CreateText(artifact.RelativePath, Restore(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())), artifact.Sources)
            : artifact);
}
#endif
