// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;

/// <summary>
/// Removes the assembly-wide Arc read protection setting rendered applications gained in #660, so earlier
/// frozen bytes remain a gate that the setting was the only change.
/// </summary>
internal static class LegacyReadProtection
{
    internal const string Setting = "<CratisReadProtection>On</CratisReadProtection>";

    internal static string Restore(string relativePath, string content)
    {
        if (!relativePath.EndsWith("Directory.Build.props", StringComparison.Ordinal)) return content;

        var index = content.IndexOf(Setting, StringComparison.Ordinal);
        if (index < 0) return content;

        var start = content.LastIndexOf('\n', index) + 1;
        var end = content.IndexOf('\n', index);
        return content.Remove(start, (end < 0 ? content.Length : end + 1) - start);
    }
}
#endif
