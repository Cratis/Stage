// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;

namespace Cratis.Stage.Host;

/// <summary>
/// The pure helpers of <see cref="StageJourneyRunner"/>: query arguments and step reports.
/// </summary>
internal static class StageJourneyStepReports
{
    /// <summary>
    /// Appends query arguments to a route, keeping any query string the route already has.
    /// </summary>
    /// <param name="route">The route.</param>
    /// <param name="arguments">The flat arguments.</param>
    /// <returns>The route with the arguments.</returns>
    internal static string WithArguments(string route, JsonElement arguments)
    {
        var query = arguments.EnumerateObject().Select(argument => $"{Uri.EscapeDataString(argument.Name)}={Uri.EscapeDataString(Scalar(argument.Value))}").ToArray();
        if (query.Length == 0) return route;

        return $"{route}{(route.Contains('?', StringComparison.Ordinal) ? '&' : '?')}{string.Join('&', query)}";
    }

    /// <summary>
    /// Reports a step with a status.
    /// </summary>
    /// <param name="step">The step.</param>
    /// <param name="status">The status.</param>
    /// <param name="watch">Times the step, or <see langword="null"/> when it did not run.</param>
    /// <param name="bindings">The journey's bindings, or <see langword="null"/>.</param>
    /// <returns>The step result.</returns>
    internal static StageJourneyStepResult Report(StageJourneyStep step, string status, Stopwatch? watch, StageJourneyBindings? bindings = null) =>
        new(step.Index, step.Name, step.Kind == StageJourneyStepKind.Command ? "command" : "query", step.Target, status, watch?.ElapsedMilliseconds ?? 0)
        {
            Expected = string.Equals(status, StageJourneyStepStatus.Failed, StringComparison.Ordinal) || string.Equals(status, StageJourneyStepStatus.Error, StringComparison.Ordinal) ? step.ExpectAsWritten : null,
            Bindings = bindings?.Snapshot(),
            Note = step.Note,
            Evidence = step.Evidence
        };

    /// <summary>
    /// Reports a step that could not be executed.
    /// </summary>
    /// <param name="step">The step.</param>
    /// <param name="watch">Times the step.</param>
    /// <param name="reason">Why the step could not be executed.</param>
    /// <param name="bindings">The journey's bindings.</param>
    /// <returns>The step result.</returns>
    internal static StageJourneyStepResult Errored(StageJourneyStep step, Stopwatch watch, string reason, StageJourneyBindings bindings) =>
        Report(step, StageJourneyStepStatus.Error, watch, bindings) with { Mismatches = [new("$", null, null, reason)] };

    /// <summary>
    /// Describes what a step answered, cutting a body over <see cref="StageJourneyRunner.ReportedBodyLimit"/> to a string.
    /// </summary>
    /// <param name="answer">The classified answer.</param>
    /// <returns>The reported answer.</returns>
    internal static StageJourneyActual Actual(StageJourneyAnswer answer)
    {
        var body = answer.Body;
        var text = body?.GetRawText();
        var truncated = text?.Length > StageJourneyRunner.ReportedBodyLimit;
        var reported = truncated ? JsonSerializer.SerializeToElement(text![..StageJourneyRunner.ReportedBodyLimit]) : body;

        return new(answer.Outcome, answer.Messages)
        {
            Response = answer.Kind == StageJourneyStepKind.Command ? reported : null,
            Result = answer.Kind == StageJourneyStepKind.Query ? reported : null,
            Truncated = truncated ? true : null
        };
    }

    static string Scalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!,
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => value.GetRawText()
    };
}
