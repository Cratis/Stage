// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Stable, bounded diagnostics for process isolation failures.</summary>
public static partial class BodyDiagnostics
{
    /// <summary>The call exceeded its wall-time deadline.</summary>
    public const string ExceededTime = "STAGE-IMPL-007";

    /// <summary>The managed heap limit or working-set watchdog was exceeded.</summary>
    public const string ExceededMemory = "STAGE-IMPL-008";

    /// <summary>The worker failed or is unavailable.</summary>
    public const string WorkerFailed = "STAGE-IMPL-009";

    [GeneratedRegex(@"(?:[A-Za-z]:[\\/]|/)[^\s]+", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex Paths { get; }

    /// <summary>Formats one sanitized line without file paths or stack frames.</summary>
    public static string Format(string code, string message)
    {
        var text = Paths.Replace(message.Replace('\r', ' ').Replace('\n', ' '), "[path]");
        var stack = text.IndexOf(" at ", StringComparison.Ordinal);
        if (stack >= 0) text = text[..stack];
        var line = $"{code}: {text}";
        return line.Length > 240 ? line[..240] : line;
    }
}
