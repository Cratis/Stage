// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Adapts one Screenplay run to its owned, isolated worker.</summary>
/// <param name="manifest">The renderer's immutable admission and compilation result.</param>
/// <param name="worker">The worker owned by this provider/run.</param>
public sealed class ScreenplayBodyExecutionProvider(BodyUnitManifest manifest, IBodyWorkerClient worker) : ISemanticBodyExecutionProvider
{
    int _disposed;

    /// <inheritdoc/>
    public async ValueTask<SemanticBodyExecutionResult> EvaluateAsync(SemanticImplementationBody body, SemanticRuleContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Volatile.Read(ref _disposed) != 0) return Unsupported("Body execution provider is disposed.");
        var entries = manifest.Entries.Where(entry => entry.RequirementId == body.Requirement.RequirementId).Take(2).ToArray();
        if (entries.Length != 1) return Unsupported("Body requirement is not uniquely present in the compilation manifest.");
        var entry = entries[0];
        if (!entry.Admitted) return new(null, entry.DiagnosticMessage);
        if (!Matches(body, entry)) return Unsupported("Body changed since compile.");
        var verdict = await worker.EvaluateAsync(new(entry.RequirementId, Members(context, entry)), cancellationToken).ConfigureAwait(false);
        return Result(verdict);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        if (worker is IAsyncDisposable disposable) await disposable.DisposeAsync().ConfigureAwait(false);
    }

    static bool Matches(SemanticImplementationBody body, BodyUnitEntry entry) =>
        body.Requirement.Role == SemanticImplementationRole.RulePredicate && entry.Role == nameof(SemanticImplementationRole.RulePredicate) &&
        string.Equals(body.Requirement.ContentHash, entry.BodySha256, StringComparison.OrdinalIgnoreCase) &&
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.Content))).Equals(entry.BodySha256, StringComparison.OrdinalIgnoreCase);

    static ImmutableDictionary<string, System.Text.Json.JsonElement> Members(SemanticRuleContext context, BodyUnitEntry entry) =>
        new Dictionary<string, SemanticValue>(StringComparer.Ordinal)
        {
            ["Artifact"] = context.Artifact,
            ["Value"] = context.Value,
            ["Property"] = SemanticValue.Text(entry.PropertyPaths.GetValueOrDefault(context.Property, context.Property)),
            ["Occurred"] = context.Occurred is { } occurred ? SemanticValue.Text(occurred.ToString("O", CultureInfo.InvariantCulture)) : SemanticValue.Null
        }.ToImmutableDictionary(pair => pair.Key, pair => SemanticValueWire.Write(pair.Value), StringComparer.Ordinal);

    static SemanticBodyExecutionResult Result(BodyEvaluation verdict) => verdict.Outcome switch
    {
        "satisfied" => new(true),
        "unsatisfied" => new(false),
        _ => new(null, verdict.Diagnostic ?? BodyDiagnostics.Format(Code(verdict.Outcome), "Body execution did not produce a predicate answer."))
    };

    static string Code(string outcome) => outcome switch
    {
        "exceeded-time" => BodyDiagnostics.ExceededTime,
        "exceeded-memory" => BodyDiagnostics.ExceededMemory,
        _ => BodyDiagnostics.WorkerFailed
    };

    static SemanticBodyExecutionResult Unsupported(string message) => new(null, BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, message));
}
