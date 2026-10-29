// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Reducer transitions: Screenplay validates the returned state and key; the worker only folds one event.</summary>
public sealed partial class ScreenplayBodyExecutionProvider
{
    /// <inheritdoc/>
    public async ValueTask<SemanticReducerBodyResult> ReduceAsync(SemanticImplementationBody body, SemanticReducerContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Refusal(body, role => role == SemanticImplementationRole.ReducerTransition, "Body changed since compile or is not a reducer transition.") is { } refusal)
            return SemanticReducerBodyResult.Unsupported(refusal);
        if (ReducerMembers(context) is not { } members)
            return SemanticReducerBodyResult.Unsupported(BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, "The reducer key or sequence number cannot be represented by the rendered reducer."));
        var verdict = await worker.ReduceAsync(new(body.Requirement.RequirementId, members), cancellationToken).ConfigureAwait(false);
        return Reduction(verdict);
    }

    static ImmutableDictionary<string, JsonElement>? ReducerMembers(SemanticReducerContext context)
    {
        // The rendered reducer receives the event source id as text and the sequence number as a checked long.
        if (ReducerKey(context.Key) is not { } key || context.SequenceNumber > long.MaxValue) return null;
        return new Dictionary<string, SemanticValue>(StringComparer.Ordinal)
        {
            ["State"] = context.State ?? SemanticValue.Null,
            ["Event"] = context.Event,
            ["Key"] = SemanticValue.Text(key),
            ["Tenant"] = SemanticValue.Text(context.Tenant ?? string.Empty),
            ["Occurred"] = context.Occurred is { } occurred ? SemanticValue.Text(occurred.ToString("O", CultureInfo.InvariantCulture)) : SemanticValue.Null,
            ["SequenceNumber"] = SemanticValue.Number(context.SequenceNumber)
        }.ToImmutableDictionary(pair => pair.Key, pair => SemanticValueWire.Write(pair.Value), StringComparer.Ordinal);
    }

    static string? ReducerKey(SemanticValue key) => key switch
    {
        SemanticTextValue text => text.Value,
        SemanticNumberValue number => number.Value.ToString(CultureInfo.InvariantCulture),
        _ => null
    };

    static SemanticReducerBodyResult Reduction(BodyEvaluation verdict)
    {
        try
        {
            return verdict.Outcome switch
            {
                "state" when verdict.Value is { } value => SemanticReducerBodyResult.Next(SemanticValueWire.Read(value)),
                "delete" => SemanticReducerBodyResult.Delete(),
                _ => SemanticReducerBodyResult.Unsupported(verdict.Diagnostic ?? BodyDiagnostics.Format(Code(verdict.Outcome), "Body execution did not produce a reducer state."))
            };
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return SemanticReducerBodyResult.Unsupported(BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, "Body execution returned a malformed reducer state."));
        }
    }
}
