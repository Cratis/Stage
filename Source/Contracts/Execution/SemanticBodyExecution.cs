// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Retains a model's compiled bodies and creates one isolated provider per semantic run.</summary>
public sealed class SemanticBodyExecution : ISemanticBodyExecutionProviderFactory, IAsyncDisposable
{
    readonly SemanticId _application;
    readonly Lazy<IBodyWorkerClient> _worker;
    int _disposed;

    SemanticBodyExecution(LoadedSemanticModel loaded, BodyUnitManifest manifest, Func<IBodyWorkerClient> worker)
    {
        _application = loaded.Model.Application.Id;
        _worker = new(worker, LazyThreadSafetyMode.ExecutionAndPublication);
        Manifest = manifest;
        Bodies = [.. loaded.ImplementationRequirements.Where(requirement => loaded.ImplementationContents.ContainsKey(requirement.RequirementId))
            .Select(requirement => new SemanticImplementationBody(requirement, loaded.ImplementationContents[requirement.RequirementId]))];
    }

    /// <summary>Gets the renderer's admission and compilation manifest.</summary>
    public BodyUnitManifest Manifest { get; }

    /// <summary>Gets the source-bound bodies retained for Screenplay's run factory.</summary>
    public ImmutableArray<SemanticImplementationBody> Bodies { get; }

    /// <summary>Combines one loaded model and manifest with an owned-worker factory.</summary>
    /// <param name="loaded">The loaded model.</param>
    /// <param name="manifest">The compilation manifest for this model revision.</param>
    /// <param name="worker">Creates the shared worker owned by this application.</param>
    /// <returns>The reusable, immutable body execution configuration.</returns>
    public static SemanticBodyExecution From(LoadedSemanticModel loaded, BodyUnitManifest manifest, Func<IBodyWorkerClient> worker) => new(loaded, manifest, worker);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        IBodyWorkerClient? worker;
        lock (_worker)
        {
            if (_disposed != 0) return;
            _disposed = 1;
            worker = _worker.IsValueCreated ? _worker.Value : null;
        }
        if (worker is IAsyncDisposable disposable) await disposable.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Checks whether one rule or validation has exactly one admitted compiled entry.</summary>
    /// <param name="requirementId">The named rule requirement.</param>
    /// <returns>Whether the rule is admitted by the renderer.</returns>
    public bool IsAdmitted(string? requirementId) =>
        Manifest.Entries.Count(entry => entry.RequirementId == requirementId) == 1 &&
        Manifest.Entries.Any(entry => entry.RequirementId == requirementId && entry.Admitted && (entry.Role == nameof(SemanticImplementationRole.RulePredicate) || entry.Role == nameof(SemanticImplementationRole.CommandValidation) || entry.Role == nameof(SemanticImplementationRole.ConceptValidation) || entry.Role == nameof(SemanticImplementationRole.ReducerTransition)));

    /// <summary>Returns the renderer's refusal without making a worker call.</summary>
    /// <param name="requirementId">The rule requirement.</param>
    /// <returns>The diagnostic, or null if none is recorded.</returns>
    public string? Refusal(string? requirementId) =>
        Manifest.Entries.FirstOrDefault(entry => entry.RequirementId == requirementId && !entry.Admitted)?.DiagnosticMessage;

    /// <inheritdoc/>
    public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_worker)
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);
            if (application.Id != _application) throw new InvalidOperationException("Body execution belongs to a different application.");
            return new(new ScreenplayBodyExecutionProvider(Manifest, new BodyWorkerLease(_worker.Value)));
        }
    }
}
