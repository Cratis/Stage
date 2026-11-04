// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>Canonical render-only metadata; passivity does not alter portable ESM bytes.</summary>
internal static class PassiveReadModelsInput
{
    internal const string Name = "cratis-realization:passive-read-models";
    const string Version = "1";

    internal static ArtifactRenderProfile With(ArtifactRenderProfile profile, ImmutableHashSet<SemanticId> identities)
    {
        // Absence is the legacy profile: do not add an empty input or perturb its digest.
        if (identities.IsEmpty)
        {
            return profile;
        }
        var input = Create([.. identities.Select(id => id.ToString()).Order(StringComparer.Ordinal)]);
        return ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs.Where(candidate => candidate.Name != Name), input]);
    }

    internal static bool TryRead(ArtifactRenderProfile profile, ExecutableSemanticModel? model, out ImmutableHashSet<SemanticId> identities)
    {
        identities = [];
        var inputs = profile.Inputs.Where(input => input.Name == Name).ToArray();
        if (inputs.Length == 0) return true;
        if (inputs.Length != 1 || inputs[0].Version != Version) return false;
        try
        {
            var input = inputs[0];
            var ids = JsonSerializer.Deserialize<string[]>(input.Bytes.AsSpan());
            if (!IsCanonical(ids, input)) return false;
            if (model is null) return true;
            var declared = SemanticVersionFeatures.Slices(model.Application).SelectMany(slice => slice.ReadModels)
                .ToDictionary(readModel => readModel.Id.ToString(), readModel => readModel.Id, StringComparer.Ordinal);
            if (ids!.Any(id => !declared.ContainsKey(id))) return false;
            identities = [.. ids!.Select(id => declared[id])];
            var passive = identities;

            // Screenplay forbids querying passive models; a manually supplied profile must not bypass that rule.
            return !SemanticVersionFeatures.Slices(model.Application).Any(slice => slice.Queries.Any(query => passive.Contains(query.ReadModel)));
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static bool IsCanonical(string[]? ids, ArtifactRenderInput input)
    {
        if (ids is not { Length: > 0 } || ids.Any(string.IsNullOrWhiteSpace)) return false;
        if (!ids.SequenceEqual(ids.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal), StringComparer.Ordinal)) return false;
        var canonical = Create(ids);
        return canonical.Sha256 == input.Sha256 && canonical.Bytes.SequenceEqual(input.Bytes);
    }

    static ArtifactRenderInput Create(string[] ids) => ArtifactRenderInput.Create(Name, Version, [.. JsonSerializer.SerializeToUtf8Bytes(ids)]);
}
