// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Retains a model's compiled bodies and creates one isolated provider per semantic run.</summary>
public sealed class SemanticBodyExecution : ISemanticBodyExecutionProviderFactory
{
    readonly SemanticId _application;
    readonly Func<IBodyWorkerClient> _worker;

    SemanticBodyExecution(LoadedSemanticModel loaded, BodyUnitManifest manifest, Func<IBodyWorkerClient> worker)
    {
        _application = loaded.Model.Application.Id;
        _worker = worker;
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
    /// <param name="worker">Creates a new owned worker for each semantic run.</param>
    /// <returns>The reusable, immutable body execution configuration.</returns>
    public static SemanticBodyExecution From(LoadedSemanticModel loaded, BodyUnitManifest manifest, Func<IBodyWorkerClient> worker) => new(loaded, manifest, worker);

    /// <summary>Checks whether one named rule has exactly one admitted compiled entry.</summary>
    /// <param name="requirementId">The named rule requirement.</param>
    /// <returns>Whether the rule is admitted by the renderer.</returns>
    public bool IsAdmitted(string? requirementId) =>
        Manifest.Entries.Count(entry => entry.RequirementId == requirementId) == 1 &&
        Manifest.Entries.Any(entry => entry.RequirementId == requirementId && entry.Admitted && entry.Role == nameof(SemanticImplementationRole.RulePredicate));

    /// <summary>Returns the renderer's refusal without making a worker call.</summary>
    /// <param name="requirementId">The rule requirement.</param>
    /// <returns>The diagnostic, or null if none is recorded.</returns>
    public string? Refusal(string? requirementId) =>
        Manifest.Entries.FirstOrDefault(entry => entry.RequirementId == requirementId && !entry.Admitted)?.DiagnosticMessage;

    /// <inheritdoc/>
    public ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (application.Id != _application) throw new InvalidOperationException("Body execution belongs to a different application.");
        return new(new ScreenplayBodyExecutionProvider(Manifest, _worker()));
    }
}
