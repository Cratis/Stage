// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

internal static class CratisScaffoldArtifacts
{
    internal static IEnumerable<PlannedArtifact> Render(ArtifactRenderProfile profile, string rootNamespace, ImmutableArray<SemanticId> sources)
    {
        var strings = StringsCatalogInput.From(profile);
        var sceneName = $"cratis-scaffold:text:{SceneCompositionInput.RelativePath}";
        foreach (var input in profile.Inputs.Where(input => input.Name != sceneName && input.Name is not StringsCatalogInput.Name and not AuthoringMetadataInput.Name and not DomainPlacementInput.Name and not ScreenPartitionsInput.Name and not ReducerConventionInput.Name and not RefusalModeInput.Name))
        {
            if (!CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact))
            {
                throw new InvalidArtifactRenderContract($"Package-owned scaffold input '{input.Name}' cannot be rendered.");
            }

            yield return artifact!.RelativePath == "Program.cs" && strings is not null
                ? PlannedArtifact.CreateText(artifact.RelativePath, StringsCatalogInput.ConfigureProgram(Encoding.UTF8.GetString(artifact.Bytes.AsSpan()), strings))
                : artifact;
        }

        if (profile.Inputs.SingleOrDefault(input => input.Name == ReducerConventionInput.Name) is { } convention)
        {
            yield return PlannedArtifact.CreateText(ReducerConventionInput.RelativePath, Encoding.UTF8.GetString(convention.Bytes.AsSpan()), sources);
        }

        if (strings is not null)
        {
            yield return PlannedArtifact.CreateText(StringsCatalogInput.RelativePath, StringsCatalogInput.Render(strings, rootNamespace), sources);
        }
    }
}
