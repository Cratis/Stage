// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;

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

    static readonly Dictionary<SemanticImplementationRole, string> _slices = new()
    {
        [SemanticImplementationRole.RulePredicate] = "S1",
        [SemanticImplementationRole.CommandValidation] = "S2",
        [SemanticImplementationRole.ConceptValidation] = "S2",
        [SemanticImplementationRole.ReducerTransition] = "S3",
        [SemanticImplementationRole.PolicyPredicate] = "S4",
        [SemanticImplementationRole.CommandHandler] = "S5",
        [SemanticImplementationRole.ReactionEffect] = "S6",
        [SemanticImplementationRole.QueryPerformer] = "S7"
    };

    [GeneratedRegex(@"(?:[A-Za-z]:[\\/]|/)[^\s]+", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex Paths { get; }

    /// <summary>Names the unexecuted body kind and its planned delivery slice.</summary>
    /// <param name="role">The body role that has no executor yet.</param>
    public static string NotExecuted(SemanticImplementationRole role) =>
        $"{role} is not executed by `cratis run` yet (Stage#891 slice {_slices.GetValueOrDefault(role, "not scheduled")}).";

    /// <summary>Formats one sanitized line without file paths or stack frames.</summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The raw message, from which paths and stack frames are removed.</param>
    public static string Format(string code, string message)
    {
        var text = Paths.Replace(message.Replace('\r', ' ').Replace('\n', ' '), "[path]");
        var stack = text.IndexOf(" at ", StringComparison.Ordinal);
        if (stack >= 0) text = text[..stack];
        var line = $"{code}: {text}";
        return line.Length > 240 ? line[..240] : line;
    }
}
