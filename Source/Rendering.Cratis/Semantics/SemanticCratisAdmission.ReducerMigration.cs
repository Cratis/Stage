// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits read models migrated from classic reducers and pairs each removed classic reducer file with its model write.
/// </summary>
internal static partial class SemanticCratisAdmission
{
    /// <summary>
    /// Validates persisted overrides and transient migration evidence against the model.
    /// </summary>
    /// <param name="context">The admitted application context.</param>
    /// <param name="diagnostics">The diagnostics to append refusals to.</param>
    /// <returns>The required removals, which the planner keeps only when the plan carries no errors.</returns>
    internal static ImmutableArray<ArtifactRemoval> AdmitReducerMigration(SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        var applicationId = context.Application.Id;
        var overrides = context.ReducerConvention.Overrides;
        foreach (var readModel in overrides.Keys.Where(readModel => MigratedReducer(context, readModel) is null).OrderBy(readModel => readModel.ToString(), StringComparer.Ordinal))
        {
            diagnostics.Add(Error("STAGE-REDUCER-001", $"The reducer convention migrates '{readModel}', which is not a read model built by exactly one reducer.", applicationId));
        }

        ReducerMigrationInput.TryRead(context.Request.Profile, out var migration);
        if (migration.Evidence.IsEmpty)
        {
            return [];
        }

        if (context.Request.Scope.Kind != ArtifactRenderScopeKind.Application || !context.Request.AdditionalScopes.IsDefaultOrEmpty)
        {
            diagnostics.Add(Error("STAGE-REDUCER-004", "Reducer migration persists the reducer convention, so it requires an application-scope plan.", applicationId));
            return [];
        }

        if (!migration.CanRemoveFiles)
        {
            diagnostics.Add(Error("STAGE-REDUCER-003", "Reducer migration removes the classic reducer files, but the host did not declare that it can remove files.", applicationId));
            return [];
        }

        var removals = ImmutableArray.CreateBuilder<ArtifactRemoval>();
        foreach (var evidence in migration.Evidence)
        {
            if (!overrides.TryGetValue(evidence.ReadModel, out var pinned) || MigratedReducer(context, evidence.ReadModel) is not { } migrated)
            {
                diagnostics.Add(Error("STAGE-REDUCER-001", $"Migration evidence names '{evidence.ReadModel}', which is not a migrated read model built by exactly one reducer.", applicationId));
                continue;
            }

            if (EvidenceMismatch(context, migrated, evidence, pinned) is { } mismatch)
            {
                diagnostics.Add(Error("STAGE-REDUCER-002", $"Migration evidence for read model '{context.ReadModels[evidence.ReadModel].Name}' is invalid: {mismatch}", evidence.ReadModel));
                continue;
            }

            removals.Add(new(evidence.RelativePath, evidence.Sha256, evidence.ReadModel));
        }

        return removals.ToImmutable();
    }

    static (LocatedSemanticSlice Slice, SemanticReducer Reducer)? MigratedReducer(SemanticApplicationContext context, SemanticId readModel)
    {
        if (!context.ReadModels.ContainsKey(readModel)) return null;
        var reducers = context.AllSlices.SelectMany(located => located.Slice.Reducers.Where(reducer => reducer.ReadModel == readModel).Select(reducer => (located, reducer))).ToArray();
        return reducers is [var single] ? single : null;
    }

    static string? EvidenceMismatch(
        SemanticApplicationContext context,
        (LocatedSemanticSlice Slice, SemanticReducer Reducer) migrated,
        ReducerMigrationEvidence evidence,
        ReducerOverride pinned)
    {
        var reducerName = Identifiers.ToPascalCase(migrated.Reducer.Name);
        var classicPath = string.Join('/', SliceNaming.FolderPath(migrated.Slice.Path).Append($"{reducerName}.cs"));
        var model = context.ReadModels[evidence.ReadModel];
        var modelType = $"{SliceNaming.Namespace(context.RootNamespace, context.DeclaringSlice(model.Id).Path)}.{Identifiers.ToPascalCase(model.Name)}";
        return true switch
        {
            _ when evidence.Refusal is { } refusal => refusal,
            _ when evidence.RelativePath != classicPath || pinned.PreviousPath != classicPath =>
                $"the reducer file must be '{classicPath}', where the classic reducer renders.",
            _ when !IsLowercaseSha256(evidence.Sha256) => "the file hash is not a lowercase SHA-256.",
            _ when string.IsNullOrWhiteSpace(evidence.ObserverId) || evidence.ObserverId != pinned.ObserverId =>
                "the reducer file's observer identity differs from the persisted observer identity.",
            _ when evidence.ReadModelType != modelType => $"the reducer file implements IReducerFor<{evidence.ReadModelType}> instead of IReducerFor<{modelType}>.",
            _ => null
        };
    }

    static bool IsLowercaseSha256(string? value) =>
        value is { Length: 64 } && value.All(character => char.IsAsciiDigit(character) || character is >= 'a' and <= 'f');
}
