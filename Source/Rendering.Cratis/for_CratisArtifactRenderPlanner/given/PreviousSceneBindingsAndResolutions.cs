// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

/// <summary>
/// Restores two later, intended frontend byte changes so earlier frozen plans remain a gate that they were the only changes:
/// aliased Scene command imports in src/bindings.ts (Cratis/Stage#816) and the @cratis/fundamentals resolution in package.json (Cratis/Stage#757).
/// </summary>
internal static partial class PreviousSceneBindingsAndResolutions
{
    /// <summary>
    /// Restores the previous bytes of a planned text artifact.
    /// </summary>
    /// <param name="relativePath">The artifact path.</param>
    /// <param name="text">The artifact text.</param>
    /// <returns>The text as the previous frozen plan emitted it.</returns>
    public static string Restore(string relativePath, string text)
    {
        if (relativePath.EndsWith("src/bindings.ts", StringComparison.Ordinal))
        {
            return AliasedCommandRegistration.Replace(AliasedCommandImport.Replace(text, "import { ${name} }"), "${name}");
        }

        return relativePath.EndsWith("package.json", StringComparison.Ordinal) ? FundamentalsResolution.Replace(text, "}\n}") : text;
    }

    [GeneratedRegex(@"import \{ (?<name>\w+) as __sceneCommand\d+ \}", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex AliasedCommandImport { get; }

    [GeneratedRegex(@"""(?<name>\w+)"": __sceneCommand\d+", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex AliasedCommandRegistration { get; }

    [GeneratedRegex(@"\},\n  ""resolutions"": \{\n    ""@cratis/fundamentals"": ""[^""]+""\n  \}\n\}", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex FundamentalsResolution { get; }
}
