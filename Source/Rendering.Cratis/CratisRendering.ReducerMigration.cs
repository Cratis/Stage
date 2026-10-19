// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis;

public static partial class CratisRendering
{
    /// <summary>
    /// Migrates read models rendered with classic reducers to self-reducing read models that keep the previous observer identity.
    /// </summary>
    /// <param name="profile">The complete application profile, with or without a persisted reducer convention.</param>
    /// <param name="applicationName">The semantic application name.</param>
    /// <param name="evidence">Evidence derived by <see cref="ReducerMigrationEvidence.FromExistingFile"/> for each classic reducer file to remove.</param>
    /// <param name="canRemoveFiles">Whether the publisher deletes each required removal atomically with its writes.</param>
    /// <returns>
    /// The profile whose persisted convention carries one self override per migrated read model, plus the transient evidence.
    /// Planning refuses the migration unless it is an application-scope plan, the host can remove files and every piece of evidence matches the model.
    /// </returns>
    /// <remarks>Migration is one way: removing an override from the persisted configuration does not restore the classic reducer.</remarks>
    /// <exception cref="InvalidArtifactRenderContract">The evidence is empty, names a read model twice, or the persisted convention is malformed.</exception>
    public static ArtifactRenderProfile WithReducerMigration(
        ArtifactRenderProfile profile,
        string applicationName,
        IEnumerable<ReducerMigrationEvidence> evidence,
        bool canRemoveFiles)
    {
        var entries = evidence.ToArray();
        if (entries.Length == 0 || entries.Any(entry => entry is null) || entries.Select(entry => entry.ReadModel).Distinct().Count() != entries.Length)
        {
            throw new InvalidArtifactRenderContract("Reducer migration requires evidence naming each migrated read model exactly once.");
        }

        if (!ReducerConventionInput.TryRead(profile, applicationName, out var convention))
        {
            throw new InvalidArtifactRenderContract("The profile carries malformed, stale or unsupported reducer convention configuration.");
        }

        var overrides = entries.Aggregate(convention.Overrides, (current, entry) => current.SetItem(entry.ReadModel, new(entry.ObserverId, entry.RelativePath)));
        var migrated = WithReducerConvention(profile, ReducerConventionInput.Create(applicationName, convention with { Overrides = overrides }));
        return ArtifactRenderProfile.Create(
            migrated.Target,
            migrated.TargetVersion,
            migrated.Renderer,
            migrated.RendererVersion,
            [.. migrated.Inputs.Where(input => input.Name != ReducerMigrationInput.Name), ReducerMigrationInput.Create(entries, canRemoveFiles)]);
    }
}
